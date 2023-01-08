using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace NRG.Services
{
    #region RegistryItem
    public class RegistryItem
    {
        #region Properties

        public string ProgramName { get; private set; }
        public string Section { get; private set; }
        public string Key { get; private set; }
        public string Setting { get; private set; }
        public string DefaultSetting { get; set; }

        #endregion

        #region Setup
        /// <summary>
        /// Creates a RegistryItem from an input of strings. To be used to Write and Read from the registry.
        /// </summary>
        /// <param name="programName">The name of the application. (e.g NRG, NRGDTM, NRGPC)</param>
        /// <param name="section">General category of your setting type. (e.g Import, DisplaySetting)</param>
        /// <param name="key">The name of your setting. (e.g PointSize, DefaultFilePath)</param>
        /// <param name="setting">The setting you want to store.</param>
        /// <param name="defaultSetting"></param>
        public RegistryItem(string programName, string section, string key, string setting, string defaultSetting = null)
        {
            ProgramName = programName;
            Section = section;
            Key = key;
            Setting = setting;
            DefaultSetting = defaultSetting;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Writes the RegistryItem to the registry with its new Setting. Returns false if an input was null or whitespace.
        /// </summary>
        /// <returns></returns>
        public bool WriteSetting(string newSetting, string newDefaultSetting = null)
        {
            //Overwrite existing setting
            Setting = newSetting;
            if (!string.IsNullOrWhiteSpace(newDefaultSetting))
                DefaultSetting = newDefaultSetting;

            //Null reference checks
            if (string.IsNullOrWhiteSpace(ProgramName) || string.IsNullOrWhiteSpace(Section) || string.IsNullOrWhiteSpace(Key))
                return false;

            //DefaultSetting check
            if (string.IsNullOrWhiteSpace(Setting) && !string.IsNullOrWhiteSpace(DefaultSetting))
                Setting = DefaultSetting;

            if (string.IsNullOrWhiteSpace(Setting))
                return false;

            //Write to Registry
            Microsoft.VisualBasic.Interaction.SaveSetting(ProgramName, Section, Key, Setting);

            return true;
        }
        /// <summary>
        /// Writes the RegistryItem to the registry using its current Setting, or if thats null, it uses its DefaultSetting if it has one set.
        /// </summary>
        /// <returns></returns>
        public bool WriteSetting()
        {
            //Null reference checks
            if (string.IsNullOrWhiteSpace(ProgramName) || string.IsNullOrWhiteSpace(Section) || string.IsNullOrWhiteSpace(Key))
                return false;

            //DefaultSetting check
            if (string.IsNullOrWhiteSpace(Setting) && !string.IsNullOrWhiteSpace(DefaultSetting))
                Setting = DefaultSetting;

            if (string.IsNullOrWhiteSpace(Setting))
                return false;

            //Write to Registry
            Microsoft.VisualBasic.Interaction.SaveSetting(ProgramName, Section, Key, Setting);

            return true;
        }

        /// <summary>
        ///  <para>Returns this RegistryItem's setting value from the registry.  </para>
        ///  <para>If the setting doesn't exist, and a default setting has been provided, a new RegistryItem is created. Otherwise this returns an empty string. </para>
        ///  <para>The RegistryItem's Settings property is updated with the new setting. </para>
        /// </summary>
        /// <returns></returns>
        public string ReadSetting()
        {

            //Null reference checks
            if (string.IsNullOrWhiteSpace(ProgramName) || string.IsNullOrWhiteSpace(Section) || string.IsNullOrWhiteSpace(Key))
                return null;

            
            //Read registry
            Setting = Microsoft.VisualBasic.Interaction.GetSetting(ProgramName, Section, Key);


            //Overwrite Setting with DefaultSetting if a Setting isn't found but a DefaultSetting is provided
            if (string.IsNullOrWhiteSpace(Setting) && !string.IsNullOrWhiteSpace(DefaultSetting))
            {
                this.WriteSetting(DefaultSetting);
            }

            return Setting;
        }


        public bool DeleteSetting()
        {
            //Null reference checks
            if (string.IsNullOrWhiteSpace(ProgramName) || string.IsNullOrWhiteSpace(Section) || string.IsNullOrWhiteSpace(Key))
                return false;

            //Delete setting
            Microsoft.VisualBasic.Interaction.DeleteSetting(ProgramName, Section, Key);
            return true;
        }



        #endregion

    }
    #endregion

    //**For most interactions with the registry you'll want to be using methods from this object. TN: 30.09.21
    #region RegistryCollection
    /// <summary>
    /// <para>A store for all registry items. There should be only one of these for the entire application. </para>
    /// <para>Creating and using multiple of these objects may result in incorrect settings being saved, stored or read from the registry.  </para>
    /// </summary>
    public class RegistryCollection
    {
        #region Properties
        private Dictionary<string, RegistryItem> RegistryDict { get;  set; } //(Debugging) Handy to see whats been loaded etc at any one given time. Has no other functionality really TN 01.06.22


        #endregion

        #region Setup

        public RegistryCollection()
        {
            RegistryDict = new Dictionary<string, RegistryItem>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a RegistryItem to your RegistryCollection. Then writes the RegistryItem to the registry. Overwrites existing settings if they exist.
        /// </summary>
        /// <param name="regItem"></param>
        /// <returns></returns>
        public bool AddRegistrySetting(RegistryItem regItem)
        {
            //Create Dictionary Key
            string dictKey = regItem.ProgramName + "," + regItem.Section + "," + regItem.Key;

            //Check for existing key and overwrite it with new settings if it exists
            if (this.RegistryDict.ContainsKey(dictKey))
            {
                var existingRegItem = this.RegistryDict[dictKey];

                //Overwrite existing setting in the registry
                if (existingRegItem.WriteSetting(regItem.Setting, regItem.DefaultSetting))
                    return true;
                else 
                    return false;
            }
            else //This is a new setting
            {
                //Write the setting to the registry and add the object to the dictionary
                if (regItem.WriteSetting())
                {
                    this.RegistryDict.Add(dictKey, regItem);
                    return true;
                }
            }
              
            return false;
        }
        /// <summary>
        /// Creates and adds a RegistryItem to your RegistryCollection. Then writes the RegistryItem to the registry. Returns false if it fails (if "Setting" & "DefaultSetting" are null).
        /// </summary>
        /// <param name="programName">The name of the application. (e.g NRG, NRGDTM, NRGPC)</param>
        /// <param name="section">General category of your setting type. (e.g Import, DisplaySetting)</param>
        /// <param name="key">The name of your setting. (e.g PointSize, DefaultFilePath)</param>
        /// <param name="setting">The setting you want to store.</param>
        /// <param name="defaultSetting"></param>
        /// <returns></returns>
        public bool AddRegistrySetting(string programName, string section, string key, string setting, string defaultSetting = null)
        {
            //Create new registry item
            RegistryItem regItem = new RegistryItem(programName, section, key, setting, defaultSetting = null);

            //Adds registry item to dictionary then writes to the registry
            if (this.AddRegistrySetting(regItem))
                return true;

            return false;
        }


        /// <summary>
        /// <para>Returns the setting from the registry and ensures the dictionary is updated.</para>
        /// <para>If a setting cannot be found with the given parameters, a new one is written to the registry and returned.</para>
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="defaultSetting"></param>
        /// <returns></returns>
        public string GetRegistrySetting(string programName, string section, string key, string defaultSetting = null)
        {
            //Create Dictionary Key
            string dictKey = programName + "," + section + "," + key;

            //Check to see if the regItem exists, if not, create a new one
            if (!this.RegistryDict.ContainsKey(dictKey))
            {
                RegistryItem regItem = new RegistryItem(programName, section, key, "", defaultSetting);
                RegistryDict.Add(dictKey, regItem);
            }

            //Overwrite DefaultSetting if a new one is provided
            if (!string.IsNullOrWhiteSpace(defaultSetting))
                this.RegistryDict[dictKey].DefaultSetting = defaultSetting;
            
            //Returns the setting from the registry, or creates a new entry in the registry if a setting can't be found
            return this.RegistryDict[dictKey].ReadSetting();
        }

        /// <summary>
        /// <para>Returns the setting from the registry and ensures the dictionary is updated.</para>
        /// <para>If a setting cannot be found with the given parameters, a new one is written to the registry and returned.</para>
        /// </summary>
        /// <param name="regItem"></param>
        /// <returns></returns>
        public string GetRegistrySetting(RegistryItem regItem)
        {
            return GetRegistrySetting(regItem.ProgramName, regItem.Section, regItem.Key, regItem.DefaultSetting = null);
        }

        /// <summary>
        /// Gets return a multi-dimentional array containing each setting's Key & Value. Also adds the setting to the RegistryController's Dictionary to keep everything in sync.
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public string[,] GetAllSettingsFromSection(string programName, string section)
		{
            var allSettings = Microsoft.VisualBasic.Interaction.GetAllSettings(programName, section);

            if (allSettings == null)
                return null;

            for (int i = 0; i < allSettings.Length / 2; i++)
			{
                AddRegistrySetting(programName, section, allSettings[i, 0], allSettings[i, 1]);
			}

            return allSettings;

        }

         /// <summary>
         /// Deletes the RegistryItem from the RegistryCollection and the Registry itself. Requires a valid ProgramName, Section & Key.
         /// </summary>
         /// <param name="regItem"></param>
         /// <returns></returns>
        public bool DeleteRegistrySetting(RegistryItem regItem)
        {
            return DeleteRegistrySetting(regItem.ProgramName, regItem.Section, regItem.Key);
        }

        /// <summary>
        /// Deletes the RegistryItem from the RegistryCollection and the Registry itself. Requires a valid ProgramName, Section & Key.
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool DeleteRegistrySetting(string programName, string section, string key)
        {
            //Null reference checks
            if (string.IsNullOrWhiteSpace(programName) || string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
                return false;

            //Create Dictionary Key
            string dictKey = programName + "," + section + "," + key;

            //Check to see if the regItem exists and removes it if it does
            if (this.RegistryDict.ContainsKey(dictKey))
            {
                var existingItem = this.RegistryDict[dictKey];

                if (!existingItem.DeleteSetting())
                    return false;

                this.RegistryDict.Remove(dictKey);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes a whole section from the registry, including all of the apropriate RegistryItems from the RegistryCollection
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public bool DeleteRegistrySection(string programName, string section)
        {
            //Null reference checks
            if (string.IsNullOrWhiteSpace(programName) || string.IsNullOrWhiteSpace(section))
                return false;

            List<string> removalList = new List<string>(); ;

            
            if (RegistryDict != null && RegistryDict.Count > 0)
            {
                //Loop through the dictionary and retrieve the dictionary keys of the ones we need to delete.
                foreach (var regItem in RegistryDict.Values)
                {
                    if (regItem.ProgramName == programName && regItem.Section == section)
                        removalList.Add(regItem.ProgramName + "," + regItem.Section + "," + regItem.Key);
                }

                //Remove selected RegistryItems from the dictionary
                foreach (var dictKey in removalList)
                    RegistryDict.Remove(dictKey);
            }

            //Check section exists
            if (Interaction.GetAllSettings(programName, section) == null || Interaction.GetAllSettings(programName, section).Length == 0)
                return false;

            //Delete section
            Microsoft.VisualBasic.Interaction.DeleteSetting(programName, section);
            return true;
        }


        #endregion
    }
    #endregion


}
