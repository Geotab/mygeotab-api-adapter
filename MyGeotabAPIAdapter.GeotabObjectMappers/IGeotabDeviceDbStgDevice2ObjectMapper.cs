﻿using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Device"/> and <see cref="DbStgDevice2"/> entities.
    /// </summary>
    public interface IGeotabDeviceDbStgDevice2ObjectMapper : ICreateOnlyGeotabObjectMapper<Device, DbStgDevice2>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="deviceGroups"/>.
        /// </summary>
        /// <param name="deviceGroups">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetDeviceGroupsJSON(IList<Group> deviceGroups);
    }
}
