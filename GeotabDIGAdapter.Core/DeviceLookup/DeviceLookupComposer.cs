namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// Composes several device lookup sources into the single map the transmit path reads.
    ///
    /// Two kinds of disagreement are possible when more than one source is active, and they are not equally serious:
    ///
    /// A single Geotab serial number appearing under two different identifiers is CORRUPTING - it would post two
    /// vehicles' telemetry under one device, silently. DeviceSubscriptions prevents this within itself, with a
    /// filtered unique index on the serial number; ProvisionedDevices has no such index and does not. Nothing
    /// prevents it across sources either, so every identifier involved is excluded. Excluding is only a safe response because a
    /// lookup miss holds the affected telemetry for a later iteration rather than discarding it.
    ///
    /// A single identifier appearing in two sources is merely AMBIGUOUS. If both sources name the same serial number
    /// there is nothing to resolve. If they disagree, one of them is stale, and the more recently changed record
    /// wins - an arbitrary but deterministic tiebreak, and the only one available that does not assume which source
    /// is authoritative.
    ///
    /// Alongside the map, the sources' withdrawn identifiers are composed into a second set, so that the transmit path
    /// can tell a device whose authorisation has been withdrawn from one nobody has ever heard of. Authorisation from
    /// any source removes an identifier from that set: a device the other control plane still authorises is
    /// transmittable, and a source that has forgotten about it is stale rather than authoritative.
    ///
    /// All comparison is ordinal. Identifier casing is never folded anywhere in this design, because casing can be
    /// the only thing separating two valid identifiers.
    /// </summary>
    public class DeviceLookupComposer : IDeviceLookupComposer
    {
        /// <inheritdoc/>
        public DeviceLookupCompositionResult Compose(IReadOnlyList<DeviceLookupContribution> contributions)
        {
            var winners = new Dictionary<string, ContributedEntry>(StringComparer.Ordinal);
            var ambiguousCollisionMessages = new List<string>();
            var corruptingCollisionMessages = new List<string>();
            var agreeingDuplicateCount = 0;

            // Every identifier any source authorised, recorded before the corrupting-collision pass removes some of
            // them. Both of the identifiers this set has to keep out of the withdrawn set are in it: one another
            // source still authorises, and one excluded by a collision. The second is the subtle one - it is absent
            // from the composed map, so a set built from the map alone would call it withdrawn and start failing
            // telemetry that the exclusion rule specifically intends to hold.
            var everAuthorizedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var contribution in contributions)
            {
                foreach (var entry in contribution.Entries)
                {
                    if (string.IsNullOrEmpty(entry.ThirdPartyId) || string.IsNullOrEmpty(entry.GeotabSerialNumber))
                    {
                        // Both columns are NOT NULL in every source table, so this is unreachable through the
                        // supported write paths. It is skipped rather than trusted because a device with no
                        // identifier or no serial number cannot be transmitted for in any case.
                        continue;
                    }

                    everAuthorizedIdentifiers.Add(entry.ThirdPartyId);

                    if (!winners.TryGetValue(entry.ThirdPartyId, out var incumbent))
                    {
                        winners[entry.ThirdPartyId] = new ContributedEntry(contribution.SourceName, entry);
                        continue;
                    }

                    if (string.Equals(incumbent.Entry.GeotabSerialNumber, entry.GeotabSerialNumber, StringComparison.Ordinal))
                    {
                        agreeingDuplicateCount++;
                        continue;
                    }

                    var challengerWins = entry.RecordLastChangedUtc > incumbent.Entry.RecordLastChangedUtc;
                    var winner = challengerWins ? new ContributedEntry(contribution.SourceName, entry) : incumbent;
                    var loser = challengerWins ? incumbent : new ContributedEntry(contribution.SourceName, entry);
                    winners[entry.ThirdPartyId] = winner;

                    ambiguousCollisionMessages.Add(
                        $"Identifier '{entry.ThirdPartyId}' is present in more than one device lookup source with a different Geotab serial number in each: "
                        + $"'{winner.Entry.GeotabSerialNumber}' in {winner.SourceName} (last changed {winner.Entry.RecordLastChangedUtc:u}) and "
                        + $"'{loser.Entry.GeotabSerialNumber}' in {loser.SourceName} (last changed {loser.Entry.RecordLastChangedUtc:u}). "
                        + $"The more recently changed record wins, so '{winner.Entry.GeotabSerialNumber}' will be used. One of the two sources is out of date.");
                }
            }

            // A serial number reached by more than one identifier is corrupting, so every identifier that reaches it
            // is excluded. This is evaluated after the ambiguity pass so that it sees the identifiers that actually
            // survived, rather than ones that were about to be replaced anyway.
            var identifiersBySerialNumber = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var winner in winners)
            {
                if (!identifiersBySerialNumber.TryGetValue(winner.Value.Entry.GeotabSerialNumber, out var identifiers))
                {
                    identifiers = new List<string>();
                    identifiersBySerialNumber[winner.Value.Entry.GeotabSerialNumber] = identifiers;
                }
                identifiers.Add(winner.Key);
            }

            var excludedDeviceCount = 0;
            foreach (var serialNumberGroup in identifiersBySerialNumber)
            {
                if (serialNumberGroup.Value.Count < 2)
                {
                    continue;
                }

                var describedIdentifiers = serialNumberGroup.Value
                    .OrderBy(identifier => identifier, StringComparer.Ordinal)
                    .Select(identifier => $"'{identifier}' ({winners[identifier].SourceName})");

                corruptingCollisionMessages.Add(
                    $"Geotab serial number '{serialNumberGroup.Key}' is claimed by {serialNumberGroup.Value.Count} identifiers: {string.Join(", ", describedIdentifiers)}. "
                    + "Transmitting for either would post more than one vehicle's telemetry under a single device, so all of them are excluded until the conflict is resolved.");

                foreach (var identifier in serialNumberGroup.Value)
                {
                    winners.Remove(identifier);
                    excludedDeviceCount++;
                }
            }

            var thirdPartyIdToSerialNoMap = winners.ToDictionary(
                winner => winner.Key,
                winner => winner.Value.Entry.GeotabSerialNumber,
                StringComparer.Ordinal);

            // First source to withdraw an identifier names it. Which one is arbitrary when two both have, but the
            // set is only ever read to explain a failure, and either name sends the operator to a table that agrees.
            var unauthorizedThirdPartyIdToSourceName = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var contribution in contributions)
            {
                foreach (var thirdPartyId in contribution.UnauthorizedThirdPartyIds)
                {
                    if (string.IsNullOrEmpty(thirdPartyId) || everAuthorizedIdentifiers.Contains(thirdPartyId))
                    {
                        continue;
                    }

                    unauthorizedThirdPartyIdToSourceName.TryAdd(thirdPartyId, contribution.SourceName);
                }
            }

            return new DeviceLookupCompositionResult
            {
                ThirdPartyIdToSerialNoMap = thirdPartyIdToSerialNoMap,
                CorruptingCollisionMessages = corruptingCollisionMessages,
                AmbiguousCollisionMessages = ambiguousCollisionMessages,
                AgreeingDuplicateCount = agreeingDuplicateCount,
                ExcludedDeviceCount = excludedDeviceCount,
                UnauthorizedThirdPartyIdToSourceName = unauthorizedThirdPartyIdToSourceName
            };
        }

        /// <summary>
        /// A <see cref="DeviceLookupEntry"/> together with the name of the source it came from.
        /// </summary>
        readonly record struct ContributedEntry(string SourceName, DeviceLookupEntry Entry);
    }
}
