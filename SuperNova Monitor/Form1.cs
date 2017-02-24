using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperNova_Monitor
{
    public partial class Form1 : Form
    {
        public bool weHaveConfig = false;
        public static dynamic jsonObj = new NovaSettings(null, null, 0, false);
        public System.Windows.Forms.Timer timer = null;
        public int refreshInterval = 180000;
        string configJson = null;
        class NovaEndpoint
        {
            public string Name { get; set; }
            public string EndpointURL { get; set; }
            public NovaEndpoint(string n, string g)
            {
                EndpointURL = g;
                Name = n;
            }
        }

        class NovaSettings
        {
            public string EndpointStash { get; set; }
            public string APIKeyStash { get; set; }
            public int ReloadInterval { get; set; }
            public bool doAutoRefresh { get; set; }
            public NovaSettings(string n, string g, int i, bool a)
            {
                EndpointStash = n;
                APIKeyStash = g;
                ReloadInterval = i;
                doAutoRefresh = a;
            }
        }

        public class Shares
        {
            public double valid { get; set; }
            public double invalid { get; set; }
            public int id { get; set; }
            public int donate_percent { get; set; }
            public int is_anonymous { get; set; }
            public string username { get; set; }
        }

        public class StatusData
        {
            public string username { get; set; }
            public Shares shares { get; set; }
            public int hashrate { get; set; }
            public int sharerate { get; set; }
        }
 
        public class Getuserstatus
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public StatusData data { get; set; }
        }
        public class NovaUserStatus
        {
            public Getuserstatus getuserstatus { get; set; }
        }

        public class PoolData
        {
            public string currency { get; set; }
            public string coinname { get; set; }
            public string cointarget { get; set; }
            public int coindiffchangetarget { get; set; }
            public string algorithm { get; set; }
            public string stratumport { get; set; }
            public string payout_system { get; set; }
            public int confirmations { get; set; }
            public double min_ap_threshold { get; set; }
            public int max_ap_threshold { get; set; }
            public string reward_type { get; set; }
            public int reward { get; set; }
            public double txfee { get; set; }
            public double txfee_manual { get; set; }
            public double txfee_auto { get; set; }
            public int fees { get; set; }
        }

        public class Getpoolinfo
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public PoolData data { get; set; }
        }

        public class NovaPoolInfo
        {
            public Getpoolinfo getpoolinfo { get; set; }
        }

        public class Gettimesincelastblock
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public int data { get; set; }
        }

        public class NovaPoolLastBlock
        {
            public Gettimesincelastblock gettimesincelastblock { get; set; }
        }

        public class Getpoolhashrate
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public float data { get; set; }
        }

        public class NovaPoolHashrate
        {
            public Getpoolhashrate getpoolhashrate { get; set; }
        }

        public class Data
        {
            public string pool_name { get; set; }
            public double hashrate { get; set; }
            public double efficiency { get; set; }
            public double progress { get; set; }
            public int workers { get; set; }
            public int currentnetworkblock { get; set; }
            public int nextnetworkblock { get; set; }
            public int lastblock { get; set; }
            public double networkdiff { get; set; }
            public double esttime { get; set; }
            public int estshares { get; set; }
            public int timesincelast { get; set; }
            public long nethashrate { get; set; }
        }

        public class Getpoolstatus
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public Data data { get; set; }
        }

        public class NovaPoolStatus
        {
            public Getpoolstatus getpoolstatus { get; set; }
        }

        public class UserBalanceData
        {
            public int confirmed { get; set; }
            public double unconfirmed { get; set; }
            public int orphaned { get; set; }
        }

        public class Getuserbalance
        {
            public string version { get; set; }
            public double runtime { get; set; }
            public UserBalanceData data { get; set; }
        }

        public class NovaUserBalance
        {
            public Getuserbalance getuserbalance { get; set; }
        }

        private void Callback(object sender, EventArgs e)
        {
            timer.Stop();
            reloadAllData();
            timer.Start();
        }

        public Form1()
        {
            InitializeComponent();
            try
            {
                configJson = File.ReadAllText("settings.json");
                //jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                weHaveConfig = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show("settings.json could not be read or doesn't exist, please modify and save your config.");
                weHaveConfig = false;
            }

            if (weHaveConfig)
            {
                Debug.WriteLine("we have config");
                JsonConvert.PopulateObject(configJson, jsonObj);
                if (jsonObj.APIKeyStash == null)
                {
                    MessageBox.Show("API Key is missing from config file! Reconfig and save.");
                } else
                {
                    textBox1.Text = jsonObj.APIKeyStash;
                }
                if (jsonObj.ReloadInterval != int.Parse(textBox2.Text))
                {
                    textBox2.Text = jsonObj.ReloadInterval.ToString();
                }
                checkBox1.Checked = jsonObj.doAutoRefresh;
                ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                refreshInterval = jsonObj.ReloadInterval;
                reloadAllData();
            }

            try
            {
                NovaEndpoint[] list = new NovaEndpoint[]  {
                                 new NovaEndpoint("MonaCoin -- https://mona.suprnova.cc/", "https://mona.suprnova.cc/"),
                                 new NovaEndpoint("ZCASH -- https://zec.suprnova.cc/", "https://zec.suprnova.cc/"),
                                 new NovaEndpoint("MarijuanaCoin -- https://mar.suprnova.cc/", "https://mar.suprnova.cc/"),
                               };
                comboBox1.DataSource = list;
                comboBox1.DisplayMember = "Name";
                comboBox1.ValueMember = "EndpointURL";
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            if (checkBox1.Checked)
            {
                timer = new System.Windows.Forms.Timer();
                timer.Interval = (refreshInterval * 60 * 1000);
                timer.Tick += new EventHandler(Callback);
                timer.Start();
            }
        }
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            if (error == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }
            return false;
        }

        private void reloadAllData()
        {
            loadUserStatus();
            loadPoolInfo();
            loadUserBalance();
        }
        class CustomIntConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(int));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JValue jsonValue = serializer.Deserialize<JValue>(reader);

                if (jsonValue.Type == JTokenType.Float)
                {
                    return (int)Math.Round(jsonValue.Value<double>());
                }
                else if (jsonValue.Type == JTokenType.Integer)
                {
                    return jsonValue.Value<int>();
                }

                throw new FormatException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        private void loadUserStatus ()
        {
            // index.php?page=api&action=getuserstatus&api_key=
            configJson = File.ReadAllText("settings.json");
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string wholestring = jsonObj.EndpointStash + "index.php?page=api&action=getuserstatus&api_key=" + jsonObj.APIKeyStash;
                    var json = wc.DownloadString(wholestring);
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new CustomIntConverter() }
                    };

                    NovaUserStatus statusObj = JsonConvert.DeserializeObject<NovaUserStatus>(json.ToString(), settings);
                    chart1.Series[0].Points[0].YValues[0] = 1000;
                    chart1.Series[0].Points[1].YValues[0] = 1000;
                    if (statusObj.getuserstatus.data.shares.valid != 0)
                    {
                        chart1.Series[0].Points[0].YValues[0] = statusObj.getuserstatus.data.shares.valid;
                    }
                    if (statusObj.getuserstatus.data.shares.invalid != 0)
                    {
                        chart1.Series[0].Points[1].YValues[0] = statusObj.getuserstatus.data.shares.invalid;
                    } else
                    {
                        chart1.Series[0].Points[1].YValues[0] = 0;
                    }
                    chart1.Update();
                    chart1.Legends[0].Title = String.Format("Shares: {0:0.00} / {1:0.00} ", statusObj.getuserstatus.data.shares.valid, statusObj.getuserstatus.data.shares.invalid);
                    label6.Text = String.Format("Hashrate  --- {0:0.000} Mh/s", statusObj.getuserstatus.data.hashrate/1000);
                    label7.Text = String.Format("Sharerate --- {0:0.0}", statusObj.getuserstatus.data.sharerate);
                    label8.Text = String.Format("Donation: {0:0.0}%", statusObj.getuserstatus.data.shares.donate_percent);
                    label9.Text = String.Format("Is Anonymous: {0}", (statusObj.getuserstatus.data.shares.is_anonymous == 1) ? "true" : "false");
                }
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void loadPoolInfo()
        {
            // index.php?page=api&action=getuserstatus&api_key=
            configJson = File.ReadAllText("settings.json");
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string wholestring = jsonObj.EndpointStash + "index.php?page=api&action=getpoolinfo&api_key=" + jsonObj.APIKeyStash;
                    var json = wc.DownloadString(wholestring);
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new CustomIntConverter() }
                    };

                    NovaPoolInfo statusObj = JsonConvert.DeserializeObject<NovaPoolInfo>(json.ToString(), settings);
                    
                    label14.Text = String.Format("--- {0}", statusObj.getpoolinfo.data.coinname);
                    label15.Text = String.Format("--- {0}", statusObj.getpoolinfo.data.cointarget);
                    label16.Text = String.Format("--- {0}", statusObj.getpoolinfo.data.algorithm);
                    label17.Text = String.Format("--- {0}", statusObj.getpoolinfo.data.fees);

                    wholestring = jsonObj.EndpointStash + "index.php?page=api&action=gettimesincelastblock&api_key=" + jsonObj.APIKeyStash;
                    json = wc.DownloadString(wholestring);

                    NovaPoolLastBlock lastBlock = JsonConvert.DeserializeObject<NovaPoolLastBlock>(json.ToString(), settings);
                    TimeSpan time = TimeSpan.FromSeconds(lastBlock.gettimesincelastblock.data);
                    string str = time.ToString(@"hh\hmm\m");
                    label18.Text = String.Format("--- {0}", str);

                    wholestring = jsonObj.EndpointStash + "index.php?page=api&action=getpoolstatus&api_key=" + jsonObj.APIKeyStash;
                    json = wc.DownloadString(wholestring);

                    NovaPoolStatus hashrate = JsonConvert.DeserializeObject<NovaPoolStatus>(json.ToString(), settings);

                    label20.Text = String.Format("--- {0:0} Mh/s", hashrate.getpoolstatus.data.hashrate/1000);
                    if (hashrate.getpoolstatus.data.efficiency > 100)
                    {
                        progressBar1.Maximum = (int)((int)hashrate.getpoolstatus.data.efficiency * 1.5);
                    }
                    if (!(hashrate.getpoolstatus.data.efficiency <= 0)) // some guy tossed almost 1mil invalid shares into monacoin, causes the below progress code to catch an exception. thanks dood
                    {
                        progressBar1.Value = (int)hashrate.getpoolstatus.data.efficiency;
                        label23.Text = String.Format("--- {0:0.00}%", hashrate.getpoolstatus.data.efficiency);
                    } 
                    else
                    {
                        label23.Text = String.Format("--- eff. <= 0");
                    }
                    if (hashrate.getpoolstatus.data.progress > 100)
                    {
                        progressBar2.Maximum = (int)((int)hashrate.getpoolstatus.data.progress*1.5);
                    }
                    progressBar2.Value = (int)hashrate.getpoolstatus.data.progress;
                    label24.Text = String.Format("--- {0:0.00}%", hashrate.getpoolstatus.data.progress);
                    label26.Text = String.Format("--- {0:0.00}", hashrate.getpoolstatus.data.workers);
                    label36.Text = String.Format("--- {0:0}", hashrate.getpoolstatus.data.networkdiff);
                    label38.Text = String.Format("--- {0:0}", hashrate.getpoolstatus.data.currentnetworkblock);
                    label40.Text = String.Format("--- {0:0}", hashrate.getpoolstatus.data.nextnetworkblock);
                    label42.Text = String.Format("--- {0:0}", hashrate.getpoolstatus.data.lastblock);
                    toolStripStatusLabel1.Text = String.Format("Est. shares {0}", hashrate.getpoolstatus.data.estshares);
                    toolStripStatusLabel2.Text = String.Format("https://{0}", hashrate.getpoolstatus.data.pool_name.ToString().ToLower());
                    time = TimeSpan.FromSeconds(hashrate.getpoolstatus.data.esttime);
                    str = time.ToString(@"hh\hmm\m");
                    label28.Text = String.Format("--- {0}", str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void loadUserBalance()
        {
            // index.php?page=api&action=getuserstatus&api_key=
            configJson = File.ReadAllText("settings.json");
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string wholestring = jsonObj.EndpointStash + "index.php?page=api&action=getuserbalance&api_key=" + jsonObj.APIKeyStash;
                    var json = wc.DownloadString(wholestring);
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new CustomIntConverter() }
                    };

                    NovaUserBalance statusObj = JsonConvert.DeserializeObject<NovaUserBalance>(json.ToString(), settings);
                    label33.Text = String.Format("--- {0:0.0000}", statusObj.getuserbalance.data.confirmed);
                    label34.Text = String.Format("--- {0:0.0000}", statusObj.getuserbalance.data.unconfirmed);
                    label35.Text = String.Format("--- {0:0.00}", statusObj.getuserbalance.data.orphaned);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string json = null;
            //dynamic jsonObj = new NovaSettings(null,null);
            try
            {
                json = File.ReadAllText("settings.json");
                JsonConvert.PopulateObject(json, jsonObj);
                //jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("API Key must not be empty!");
                return;
            }
            if ((json == null) && (jsonObj == null))
            {
                 jsonObj = new NovaSettings[]  {
                                new NovaSettings(comboBox1.SelectedValue.ToString(), textBox1.Text.ToString(), Int32.Parse(textBox2.Text.ToString()), checkBox1.Checked),
                           };
            }

            if (jsonObj.APIKeyStash == null)
            {
                Debug.WriteLine("writing apikey to config");
                jsonObj.APIKeyStash = textBox1.Text.ToString();
            }
            if (jsonObj.EndpointStash == null)
            {
                jsonObj.EndpointStash = comboBox1.SelectedValue.ToString();
            }

            int x = int.Parse(textBox2.Text);
            jsonObj.ReloadInterval = x;

            if (jsonObj.doAutoRefresh == false)
            {
                jsonObj.doAutoRefresh = checkBox1.Checked;
            }
            try
            {
                using (StreamWriter file = File.CreateText(@"settings.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, jsonObj);
                }
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            MessageBox.Show("Configuration written to settings.json");
            timer.Stop();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = (jsonObj.ReloadInterval * 60 * 1000);
            timer.Tick += new EventHandler(Callback);
            timer.Start();
        }

        private void toolStripContainer1_TopToolStripPanel_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(toolStripStatusLabel2.Text.ToString().ToLower());
        }
    }
}
