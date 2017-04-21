using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace XsdToClass
{
    /// <summary>
    /// Provides the command line options of the xsd converter.
    /// </summary>
    public sealed class CommandLineOptions
    {
        #region #### CONSTANTS ##########################################################
        #endregion
        #region #### DEPENDENCY DECLARATIONS ############################################
        #endregion
        #region #### VARIABLES ##########################################################
        #endregion
        #region #### PROPERTIES #########################################################
        /// <summary>
        /// Retrieves a enumeration with input files to be processed.
        /// </summary>
        [Value(0, Required = true, HelpText = "Input xsd files to be processed.")]
        public IEnumerable<String> InputFiles { get; set; }
        /// <summary>
        /// Specifies if the jupiter dependency system should be implemented for each property.
        /// </summary>
        [Option("jupiterProperties", Default = false, Required = false, HelpText = "Specifies if the jupiter dependency system should be used.")]
        public Boolean JupiterProperties { get; set; }
        /// <summary>
        /// Specifies if the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface should be implemented.
        /// </summary>
        [Option("componentModelProperties", Default = false, Required = false, HelpText = "Specifies if INotifyPropertyChanged should be implemented.")]
        public Boolean ComponentModelProperties { get; set; }
        /// <summary>
        /// Specifies that data contracts should be used.
        /// </summary>
        [Option("dataContracts", Default = false, Required = false, HelpText = "Specifies if data contract attributes should be emitted.")]
        public Boolean DataContracts { get; set; }
        /// <summary>
        /// Specifies additional include directories for other xsd files when they cannot be found.
        /// </summary>
        [Option('i', "includeDirectories", Required = false, HelpText = "Specifies if the jupiter dependency system should be used.")]
        public IEnumerable<String> IncludeUris { get; set; }
        /// <summary>
        /// Specifies if the names of elements and properties should be cleaned.
        /// </summary>
        [Option("cleanNames", Default = true, Required = false, HelpText = "Specifies if the name of elements and properties should be cleaned.")]
        public Boolean CleanNames { get; set; }
        /// <summary>
        /// Specifies if files should only contain a single class/enum or other element.
        /// </summary>
        [Option("singleElementFiles", Default = false, Required = false, HelpText = "Specifies if files should only contain a single class/enum or other element.")]
        public Boolean SingleElementFiles { get; set; }
        /// <summary>
        /// Specifies the namespace for all classes.
        /// </summary>
        [Option("targetNamespace", Default = "Unknown", Required = false, HelpText = "Specifies the namespace for all classes.")]
        public String TargetNamespace { get; set; }
        /// <summary>
        /// Specifies the pattern for members which must be renamed when having the same name as the enclosing type.
        /// </summary>
        [Option("renamingPattern", Default = "{0}Data", Required = false, HelpText = "Specifies the pattern for members which must be renamed when having the same name as the enclosing type.")]
        public String RenamingPattern { get; set; }
        #endregion
        #region #### EVENTS #############################################################
        #endregion
        #region #### CTOR ###############################################################
        #endregion
        #region #### PUBLIC #############################################################
        #endregion
        #region #### PRIVATE ############################################################
        #endregion
        #region #### NESTED TYPES #######################################################
        #endregion
    }
}