using Geotab.Checkmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// A container class that holds a <see cref="API"/> instance.
    /// </summary>
    public class MyGeotabAPIContainer : IMyGeotabAPIContainer
    {
        /// <inheritdoc/>
        public API MyGeotabAPI { get; }

        public MyGeotabAPIContainer()
        { 
        
        }
    }
}
