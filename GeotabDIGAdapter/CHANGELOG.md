# Change Log

This file tracks changes to the Geotab DIG Adapter over time, listed in reverse chronological order.

## Get Notified About New Releases

Any time a new release of the MyGeotab API Adapter solution (which includes the Geotab DIG Adapter) is published to GitHub, an update will be posted to Geotab's [Integrator's Hub](https://community.geotab.com/s/integrators-hub?language=en_US). Click the **Join Group** button on the page to join and then choose the desired notification frequency (Every Post, Daily Digest, Weekly Digest, etc.)

## Feedback

Help us prioritize future efforts and better understand how the Geotab DIG Adapter is used. If you would like to provide any feedback about the Geotab DIG Adapter, please feel free to complete the 100% voluntary [Geotab DIG Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSfMMnFxiaTuaw222-3OaA2tOATRDGnQJGA-rrBo48VM51fcRQ/viewform?usp=header).

---

## Version 5.0.0.3

- **NOTE:** There are **no database schema or configuration file changes from version 5.0.0.1 to version 5.0.0.3**. It is safe to upgrade from version 5.0.0.1 to version 5.0.0.3 by simply downloading the new version and overwriting the respective `appsettings.json` and `nlog.config` files with those that were configured for version 5.0.0.1.
  - To upgrade an existing installation from version 5.0.0.1 to version 5.0.0.3, see the [Upgrade Guide](docs/upgrade-guides/v5.0.0.3.md).
- **Bug Fix:** Resolved an issue whereby the Geotab DIG Adapter, upon receiving a `401 Unauthorized` or `403 Forbidden` token-rejection response from the DIG API, retried the same rejected bearer token indefinitely (approximately every 10 minutes) instead of re-authenticating — silently halting all delivery of records to MyGeotab until the adapter was manually restarted. The thrown error message now includes the numeric HTTP status code, so the existing token-expiry re-authentication retry policy routes the response correctly and recovers automatically (refreshing the token, or performing a full re-login as a fallback).
- **Added proactive token refresh-ahead** as defense-in-depth. The Geotab DIG Adapter now re-authenticates automatically once its DIG bearer token is within roughly an hour of expiry (this buffer is capped at half the token's lifetime, so short-lived tokens are not refreshed prematurely), rather than waiting for a token rejection. This is built-in behavior that requires no configuration; the reactive re-authentication retry described above remains in place as a backstop.
- Updated version to 5.0.0.3.

## Version 5.0.0.1

- **NOTE:** There are **no changes to the Geotab DIG Adapter in version 5.0.0.1**. The version is updated for alignment with the broader MyGeotab API Adapter solution, in which a bug fix was made to a different component (the MyGeotab API Adapter — see its [CHANGELOG](../MyGeotabAPIAdapter/CHANGELOG.md) for details). The Geotab DIG Adapter binaries are functionally identical between v5.0.0 and v5.0.0.1; existing v5.0.0 installations do not need to be upgraded. See the [Upgrade Guide](docs/upgrade-guides/v5.0.0.1.md).
- Updated version to 5.0.0.1.

## Version 5.0.0

- Introduced the Geotab DIG Adapter into the MyGeotab API Adapter solution. See the [README](README.md) for details. v5.0.0 is the initial release of the Geotab DIG Adapter — see the [Upgrade Guide](docs/upgrade-guides/v5.0.0.md).
