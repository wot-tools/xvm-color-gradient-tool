using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace XVMCGT
{
    [Serializable()]
    public class Settings : INotifyPropertyChanged
    {
        public bool StartMaximized { get; set; }
        public bool RoundValuesToInteger { get; set; }

        public Settings()
        {

        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

    }

    public class XMLManager
    {
        public static string file_settings = "settings";
        public static string file_settings_invalid = file_settings + "_invalid";
        public static string file_settings_backup = file_settings + "_backup";

        public static void Save_Xml(string filename, object item)
        {
            XmlSerializer SerializerObj = new XmlSerializer(item.GetType());

            TextWriter WriteFileStream = new StreamWriter(XMLManager.GetXMLPath(filename));
            SerializerObj.Serialize(WriteFileStream, item);

            WriteFileStream.Close();
        }
        public static void Save(string file, object item)
        {
            XmlSerializer SerializerObj = new XmlSerializer(item.GetType());

            TextWriter WriteFileStream = new StreamWriter(file);
            SerializerObj.Serialize(WriteFileStream, item);

            WriteFileStream.Close();
        }

        public static void Save(Settings item)
        {
            Save_Xml(file_settings, item);
        }

        public static Settings Load_Settings()
        {
            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(Settings));

                FileStream ReadFileStream = new FileStream(XMLManager.GetXMLPath(file_settings), FileMode.Open, FileAccess.Read, FileShare.Read);

                Settings LoadedObj = (Settings)SerializerObj.Deserialize(ReadFileStream);

                ReadFileStream.Close();

                //File.Copy(XMLManager.GetXMLPath(file_settings), XMLManager.GetXMLPath(file_settings_backup), true);

                return LoadedObj;
            }
            catch
            {
                //WRALog.WriteError(String.Format("Failed to load {0}.xml. Loading the backup instead and backing up the invalid one to {1}.xml. ({2})"
                //                                , file_settings, file_settings_invalid, excp.Message));
                //return Load_BackUpSettings();
                return GetDefaultSettings();
            }
        }
        public static Settings Load_BackUpSettings()
        {
            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(Settings));

                FileStream ReadFileStream = new FileStream(XMLManager.GetXMLPath(file_settings_backup), FileMode.Open, FileAccess.Read, FileShare.Read);

                Settings LoadedObj = (Settings)SerializerObj.Deserialize(ReadFileStream);

                ReadFileStream.Close();

                return LoadedObj;
            }
            catch
            {
                //WRALog.WriteError(String.Format("Failed to load {0}.xml. Loading the default one instead and backing up the invalid one to {1}.xml. ({2})"
                //                                , file_settings, file_settings_invalid, excp.Message));

                if (File.Exists(XMLManager.GetXMLPath(file_settings_backup)))
                    File.Copy(XMLManager.GetXMLPath(file_settings_backup), XMLManager.GetXMLPath(file_settings_invalid), true);

                Settings settings = XMLManager.GetDefaultSettings();
                //Save(settings);

                return settings;
            }
        }

        public static Settings GetDefaultSettings()
        {
            Settings settings = new Settings();

            settings.StartMaximized = true;
            settings.RoundValuesToInteger = true;

            return settings;
        }

        public static void CreateDefaultSettings()
        {
            Save(XMLManager.GetDefaultSettings());
        }
        public static string GetXMLPath(string Filename)
        {
            string path = "";
            path = System.AppDomain.CurrentDomain.BaseDirectory + Filename + ".xml";
            return path;
        }
    }
}
