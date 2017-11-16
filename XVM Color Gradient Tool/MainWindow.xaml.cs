using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace XVMCGT
{
    public partial class MainWindow : Window
    {
        public Settings settings { get; set; }
        private List<ValueItem> gradientlist_source = new List<ValueItem>();
        private Dictionary<string, string> dict_colorscalenames = new Dictionary<string, string>();
        private string sourcegradient_listname = "";
        JObject jo_config;

        public static string defaultlistname = "id_placeholder";

        #region Window
        public MainWindow()
        {
            try
            {
                DataContext = this;
                XVMCGTLog.CreateLogFile();
                XVMCGTLog.WriteInfo(String.Format("{0} v.{1} started", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));

                settings = XMLManager.Load_Settings();

                InitializeComponent();

                this.Title = String.Format("{0} v.{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());
                this.Title = this.Title.Trim();

                GenerateColorScaleNameList();


                if (settings.StartMaximized)
                    this.WindowState = WindowState.Maximized;
            }
            catch (Exception excp)
            {
                XVMCGTLog.WriteError("Encountered an error while opening the main window.", excp);
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareGradientDataGrid(DG_Gradient_Source);
            PrepareGradientDataGrid(DG_Values);

            UpdateSourceGradient();
            UpdateGradient();

            DTB_Json_Source.Text = GetDefaultGradientString();
            DTB_Json_Source.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            XVMCGTLog.WriteInfo("Closing Tool");
            XVMCGTLog.WriteInfo(String.Format("Saving settings to {0}", XMLManager.GetXMLPath("settings")));
            XMLManager.Save(settings);
            
        }
        #endregion

        #region process source data
        private void UpdateSourceGradient()
        {
            //XVMCGTLog.WriteInfo("Updating source gradient from json");
            gradientlist_source = GetSourceList();

            //XVMCGTLog.WriteInfo("Display source gradient in data grid");
            DisplayGradientInDataGrid(gradientlist_source, DG_Gradient_Source);
            TB_Header_Gradient_Source.Text = String.Format("Gradient ({0} items)", gradientlist_source.Count);
        }

        private List<ValueItem> GetSourceList()
        {
            try
            {
                List<ValueItem> sourcelist = new List<ValueItem>();

                if (!String.IsNullOrWhiteSpace(DTB_Json_Source.Text))
                {
                    string sourcestring = DTB_Json_Source.Text.Trim();
                    sourcestring = sourcestring.StartsWith("{") ? "{\"" + defaultlistname + "\": [" + sourcestring + "]}" : "{" + sourcestring + "}";

                    JObject jo = JObject.Parse(sourcestring);

                    JProperty jp = (JProperty)jo.First;
                    sourcegradient_listname = jp.Name;

                    JArray ja = (JArray)jp.Value;

                    foreach (JObject jobj in ja.Where(x=>x.Type==JTokenType.Object))
                    {
                        ValueItem item = new ValueItem();

                        item.Value = Convert.ToDouble(jobj["value"].ToString());
                        item.HexCode = jobj["color"].ToString().Remove(0, 2);
                        item.Color = (Color)ColorConverter.ConvertFromString("#" + item.HexCode);
                        item.Brush = new SolidColorBrush(item.Color);

                        sourcelist.Add(item);
                    }

                    DTB_Json_Source.Background = Brushes.White;
                }

                return sourcelist;
            }
            catch (Exception excp)
            {
                DTB_Json_Source.Background = GetErrorBrush();
                //XVMCGTLog.WriteError("Could not parse source json", excp);

                return gradientlist_source;
            }
        }

        private void DTB_Json_Source_DelayedTextChanged(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdateSourceGradient();
                UpdateGradient();
            }
        }

        private void CDUP_Steps_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                UpdateGradient();
        }

        private void CB_Json_Config_ColorType_Source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CB_Json_Config_ColorType_Source.SelectedItem != null)
            {
                ColorScaleItem item = (ColorScaleItem)CB_Json_Config_ColorType_Source.SelectedItem;
                string newid = item.ID;

                if (newid != "")
                {
                    sourcegradient_listname = newid;
                    DTB_Json_Source.Text = String.Format("\"{0}\": {1}", sourcegradient_listname, jo_config["colors"][sourcegradient_listname].ToString());
                }
            }
        }

        private void BT_Json_OpenConfigFile_Source_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "XVM config (*.xc)|*.xc|All Files (*.*)|*.*";


            System.Windows.Forms.DialogResult result = ofd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    TB_ConfigPath.Text = ofd.FileName;
                    TB_ConfigPath.Background = Brushes.White;
                    CB_Json_Config_ColorType_Source.IsEnabled = true;

                    StreamReader reader = new StreamReader(ofd.FileName);
                    string s = reader.ReadToEnd();
                    jo_config = JObject.Parse(s);

                    List<ColorScaleItem> colorscales = new List<ColorScaleItem>();

                    foreach (JProperty jprop in jo_config["colors"].Children())
                    {
                        if (jprop.Value.GetType() == typeof(JArray))
                        {
                            string name = GetColorScaleName(jprop.Name);
                            colorscales.Add(new ColorScaleItem(jprop.Name, String.Format("{0} ({1})", String.IsNullOrEmpty(name) ? "?" : name, jprop.Name)));

                        }
                    }

                    //foreach (string id in GetDefaultPossibleColorScales())
                    //{
                    //    if (((JObject)jo_config["colors"]).TryGetValue(id, out jttemp))
                    //        colorscales.Add(id);
                    //}
                    colorscales.Add(new ColorScaleItem("", ""));
                    colorscales = colorscales.OrderBy(x => x.ID).ToList();

                    CB_Json_Config_ColorType_Source.IsEnabled = true;
                    CB_Json_Config_ColorType_Source.ItemsSource = colorscales;
                    CB_Json_Config_ColorType_Source.SelectedIndex = 0;
                }
                catch
                {
                    MessageBox.Show("Encountered an error while parsing the config file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TB_ConfigPath.Text = "Could not parse config file";
                    TB_ConfigPath.Background = GetErrorBrush();
                    CB_Json_Config_ColorType_Source.ItemsSource = new List<ColorScaleItem>() { new ColorScaleItem("", "") };
                    CB_Json_Config_ColorType_Source.SelectedIndex = 0;
                    CB_Json_Config_ColorType_Source.IsEnabled = false;
                }
            }
        }
        #endregion

        #region Helper
        private void DisplayGradientInDataGrid(List<ValueItem> gradient, DataGrid datagrid)
        {
            datagrid.ItemsSource = gradient;
            //datagrid.Items.SortDescriptions.Clear();
            //datagrid.Items.SortDescriptions.Add(new SortDescription("Value", ListSortDirection.Ascending));
            //datagrid.Columns[2].SortDirection = ListSortDirection.Ascending;
        }

        private void PrepareGradientDataGrid(DataGrid datagrid)
        {
            datagrid.Items.SortDescriptions.Add(new SortDescription("Value", ListSortDirection.Ascending));
            datagrid.Columns[2].SortDirection = ListSortDirection.Ascending;
        }

        private string GetDefaultGradientString()
        {
            string s = "";

            s += "\"wn\": [\n";
            s += "{ \"value\": 300, \"color\": \"0xBAAAAD\" },\n";
            s += "{ \"value\": 450, \"color\": \"0xf11919\" },\n";
            s += "{ \"value\": 500, \"color\": \"0xff8a00\" },\n";
            s += "{ \"value\": 900, \"color\": \"0xe6df27\" },\n";
            s += "{ \"value\": 1200, \"color\": \"0x77e812\" },\n";
            s += "{ \"value\": 1600, \"color\": \"0x459300\" },\n";
            s += "{ \"value\": 2000, \"color\": \"0x2ae4ff\" },\n";
            s += "{ \"value\": 2450, \"color\": \"0x00a0b8\" },\n";
            s += "{ \"value\": 2900, \"color\": \"0xc64cff\" },\n";
            s += "{ \"value\": 9999, \"color\": \"0x8225ad\" },\n";

            //s += "{ \"value\": 300, \"color\": \"0x555555\" },\n";
            //s += "{ \"value\": 301, \"color\": \"0x999999\" },\n";

            s += "]";

            return s;
        }

        private List<string> GetDefaultPossibleColorScales()
        {
            List<string> list = new List<string>();

            list.Add("hp");
            list.Add("hp_ratio");
            list.Add("x");
            list.Add("eff");
            list.Add("wn6");
            list.Add("wn8");
            list.Add("wgr");
            list.Add("e");
            list.Add("rating");
            list.Add("kb");
            list.Add("avglvl");
            list.Add("t_battles");
            list.Add("tdb");
            list.Add("tdv");
            list.Add("tfb");
            list.Add("tsb");
            list.Add("wn8effd");
            list.Add("wn");
            list.Add("damageRating");
            list.Add("hitsRatio");

            return list;
        }

        private SolidColorBrush GetErrorBrush()
        {
            SolidColorBrush scb = new SolidColorBrush(Colors.Yellow);
            scb.Opacity = 0.5;

            return scb;
        }

        private void GenerateColorScaleNameList()
        {
            dict_colorscalenames.Add("hp", "Absolute health");
            dict_colorscalenames.Add("hp_ratio", "Health % remaining");
            dict_colorscalenames.Add("x", "XVM scale");
            dict_colorscalenames.Add("eff", "Efficiency");
            dict_colorscalenames.Add("wn6", "WN6");
            dict_colorscalenames.Add("wn8", "WN8");
            dict_colorscalenames.Add("wgr", "WG rating");
            dict_colorscalenames.Add("e", "TEFF (E) rating");
            dict_colorscalenames.Add("rating", "Win ratio");
            dict_colorscalenames.Add("kb", "Kilo battles");
            dict_colorscalenames.Add("avglvl", "Average tier");
            dict_colorscalenames.Add("t_battles", "Battles on current tank");
            dict_colorscalenames.Add("tdb", "Avg. damage on current tank");
            dict_colorscalenames.Add("tdv", "Avg. damage efficiency on current tank");
            dict_colorscalenames.Add("tfb", "Average kills");
            dict_colorscalenames.Add("tsb", "Average spots");
            dict_colorscalenames.Add("wn8effd", "WN8 effective damage");
            dict_colorscalenames.Add("damageRating", "Marks on Gun %");
            dict_colorscalenames.Add("hitsRatio", "Hit ratio");
        }
        private string GetColorScaleName(string id)
        {
            if (!String.IsNullOrEmpty(id))
                if (dict_colorscalenames.Any(x => x.Key == id))
                    return dict_colorscalenames.First(x => x.Key == id).Value;
                else
                    return "";
            else
                return "";
        }

        private string GetFinalValueString(List<ValueItem> items)
        {
            List<string> values = new List<string>();

            foreach (ValueItem item in items)
            {
                //values.Add(String.Format(format_singlevalue, item.Value.ToString(), item.HexCode));
                values.Add("{ \"value\": " + item.Value.ToString(CultureInfo.GetCultureInfo("en-GB")) + ", \"color\": \"0x" + item.HexCode + "\" }");
            }

            return String.Join(",\n", values);
        }

        private byte GetByteForColor(double d)
        {
            return (byte)Convert.ToInt32(Math.Round(d, MidpointRounding.AwayFromZero));
        }
        private Color GetColor(double a, double r, double g, double b)
        {
            return Color.FromArgb(GetByteForColor(a), GetByteForColor(r), GetByteForColor(g), GetByteForColor(b));
        }
        private string GetHexCode(Color color, bool alphaChannel)
        {
            return String.Format("{0}{1}{2}{3}",
                                 alphaChannel ? color.A.ToString("X2") : String.Empty,
                                 color.R.ToString("X2"),
                                 color.G.ToString("X2"),
                                 color.B.ToString("X2"));
        }
        #endregion

        private void UpdateGradient()
        {
            List<ValueItem> finalvalues = new List<ValueItem>();

            List<ValueItem> sourcelist = gradientlist_source; // GetSourceList();
            double steps = CDUP_Steps.Value + 1;

            JArray array = new JArray();
            JObject jsonobj = new JObject();

            #region Add steps and build json array
            for (int i = 0; i < sourcelist.Count - 1; i++)
            {
                double step_r = (sourcelist[i + 1].Color.R - sourcelist[i].Color.R) / steps;
                double step_g = (sourcelist[i + 1].Color.G - sourcelist[i].Color.G) / steps;
                double step_b = (sourcelist[i + 1].Color.B - sourcelist[i].Color.B) / steps;
                double step_a = (sourcelist[i + 1].Color.A - sourcelist[i].Color.A) / steps;

                double step_value = (sourcelist[i + 1].Value - sourcelist[i].Value) / steps;

                //if (settings.RoundValuesToInteger)
                //{
                //    step_r = Math.Round(step_r);
                //    step_g = Math.Round(step_g);
                //    step_b = Math.Round(step_b);
                //    step_a = Math.Round(step_a);
                //    step_value = Math.Round(step_value);
                //}

                for (double j = 0; j <= steps; j++)
                {
                    Color c; 

                    ValueItem item = new ValueItem();

                    item.Value = sourcelist[i].Value + j * step_value;
                    if (settings.RoundValuesToInteger)
                        item.Value = Math.Round(item.Value, MidpointRounding.AwayFromZero);


                    if (settings.RoundValuesToInteger && j != 0 && j != steps)
                    {
                        double intpolstep = (item.Value - sourcelist[i].Value) / (sourcelist[i + 1].Value - sourcelist[i].Value);

                        double test = sourcelist[i].Color.R + intpolstep * step_r * steps;
                        c = GetColor(sourcelist[i].Color.A + intpolstep * step_a * steps, test,
                                     sourcelist[i].Color.G + intpolstep * step_g * steps, sourcelist[i].Color.B + intpolstep * step_b * steps);
                    }
                    else
                        c = GetColor(sourcelist[i].Color.A + j * step_a, sourcelist[i].Color.R + j * step_r, sourcelist[i].Color.G + j * step_g, sourcelist[i].Color.B + j * step_b);

                    item.Color = c;
                    item.Brush = new SolidColorBrush(c);
                    item.HexCode = GetHexCode(c, false);

                    if (!finalvalues.Any(x => x.Value == item.Value))
                    {
                        finalvalues.Add(item);

                        JObject jo = new JObject();

                        jo["value"] = item.Value;
                        jo["color"] = "0x" + item.HexCode.ToLower();
                        array.Add(jo);
                    }
                }
            }
            #endregion

            jsonobj[sourcegradient_listname] = array;

            string s = "";
            if (finalvalues.Count > 0)
            {
                s = jsonobj.ToString(Formatting.Indented);
                s = s.Substring(1, s.Length - 2).Trim();
            }
            else
                s = "no json found for source gradient";

            DG_Values.ItemsSource = finalvalues;
            TBox_FinalValues.Text = s;
            TB_Header_Gradient.Text = String.Format("Gradient ({0:N0} steps)", finalvalues.Count);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateGradient();
        }
    }

    public class ValueItem
    {
        public Color Color { get; set; }
        public Brush Brush { get; set; }
        public string HexCode { get; set; }
        public double Value { get; set; }

        public ValueItem()
        {

        }
    }

    public class ColorScaleItem
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public ColorScaleItem()
        { }

        public ColorScaleItem(string id, string name)
        {
            ID = id;
            Name = name;
        }
    }
}
