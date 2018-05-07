using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace schistory
{
    public partial class Form1 : Form
    {
        private string progName = "schistory";
        private string host = "schistory.space";
        private int port = 4800;
        private ProgramSettings mySettings = new ProgramSettings();
        private string myPath = Assembly.GetExecutingAssembly().Location.Remove(Assembly.GetExecutingAssembly().Location.LastIndexOf("\\") + 1);
        private bool isClose = false;

        private List<NicknameUid> nicknameBase = new List<NicknameUid>();
        private List<SC> dataBase = new List<SC>();

        private List<NicknameFromChat> nicknameListFromChat = new List<NicknameFromChat>();
        private List<string> nicknameBigList = new List<string>();
        private List<string> saveNicknameBigList = new List<string>();
        private List<string> ignorNicknameBigList = new List<string>();
        private bool isWorkinWithNicknameBigString = false;
        private FileStream gamelog_file = null;
        private StreamReader gamelog_reader = null;
        private int cycle, log_cycle;
        private string good_log_old;


        public Form1(string[] args)
        {
            InitializeComponent();

            // Run us minimized
            if (args.Length > 0 && args[0] == "-m")
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            // Delete old logs
            if (File.Exists(myPath + progName + ".log"))
            {
                File.Delete(myPath + progName + ".log");
            }

            // Create modules directory if exist
            if (Directory.Exists(myPath + "modules\\") == false)
            {
                Directory.CreateDirectory(myPath + "modules\\");
            }
            if (File.Exists(myPath + "modules\\" + "ExampleModule.dll") == false)
            {
                File.WriteAllBytes(myPath + "modules\\" + "ExampleModule.dll", schistory.Properties.Resources.ExampleModule);
            }

            // Refresh modules list
            button11_Click(null, null);

            // Create dll files
            if (File.Exists(myPath + "Newtonsoft.Json.dll") == false)
            {
                File.WriteAllBytes(myPath + "Newtonsoft.Json.dll", schistory.Properties.Resources.Newtonsoft_Json);
            }

            // Defoult checkt
            mySettings.DataSourceList = new List<string>();
            checkedListBox3.SetItemChecked(0, true);

            // Restore settings
            if (File.Exists(myPath + "settings.data"))
            {
                mySettings = BytesToProgramSettings(File.ReadAllBytes(myPath + "settings.data"));

                checkBox2.Checked = mySettings.runWithSystem;
                checkBox3.Checked = mySettings.autoUpdateHistory;
                numericUpDown1.Value = mySettings.hours;
                numericUpDown2.Value = mySettings.minutes;

                if (mySettings.columnsList != null)
                {
                    List<int> indexes = new List<int>();
                    foreach (var item in checkedListBox1.Items)
                    {
                        if (mySettings.columnsList.Contains(item.ToString()))
                        {
                            indexes.Add(checkedListBox1.Items.IndexOf(item));
                        }
                    }
                    foreach (var index in indexes)
                    {
                        checkedListBox1.SetItemChecked(index, true);
                    }
                }
                if (mySettings.DataSourceList != null)
                {
                    List<int> indexes = new List<int>();
                    foreach (var item in checkedListBox3.Items)
                    {
                        if (mySettings.DataSourceList.Contains(item.ToString()))
                        {
                            indexes.Add(checkedListBox3.Items.IndexOf(item));
                        }
                    }
                    foreach (var index in indexes)
                    {
                        checkedListBox3.SetItemChecked(index, true);
                    }
                }
            }
            
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.ReadOnly = true;
            dataGridView2.ReadOnly = true;
            UpdateDataGridView1Columns();
            UpdateDataGridView2Columns();

            // Assign an image to the button.
            button1.Image = new Bitmap(schistory.Properties.Resources.icon_plus);
            button2.Image = new Bitmap(schistory.Properties.Resources.icon_minus);
            button3.Image = new Bitmap(schistory.Properties.Resources.icon_find);
            button4.Image = new Bitmap(schistory.Properties.Resources.icon_save);
            button5.Image = new Bitmap(schistory.Properties.Resources.icon_admin);
            button7.Image = new Bitmap(schistory.Properties.Resources.icon_plus);
            button8.Image = new Bitmap(schistory.Properties.Resources.icon_minus);
            button10.Image = new Bitmap(schistory.Properties.Resources.icon_ignor);
            // Align the image and text on the button.
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.TextAlign = ContentAlignment.MiddleRight;
            button2.ImageAlign = ContentAlignment.MiddleLeft;
            button2.TextAlign = ContentAlignment.MiddleRight;
            button3.ImageAlign = ContentAlignment.MiddleLeft;
            button3.TextAlign = ContentAlignment.MiddleRight;
            button4.ImageAlign = ContentAlignment.MiddleLeft;
            button4.TextAlign = ContentAlignment.MiddleRight;
            button5.ImageAlign = ContentAlignment.MiddleLeft;
            button5.TextAlign = ContentAlignment.MiddleRight;
            button7.ImageAlign = ContentAlignment.MiddleLeft;
            button7.TextAlign = ContentAlignment.MiddleRight;
            button8.ImageAlign = ContentAlignment.MiddleLeft;
            button8.TextAlign = ContentAlignment.MiddleRight;
            button10.ImageAlign = ContentAlignment.MiddleLeft;
            button10.TextAlign = ContentAlignment.MiddleRight;

            UpdateTable1();
            UpdateNicknamesManagement(null, null);

            // Visible settings
            UpdateVisibleSettings();

            

            // Run an additional thread Schedule
            Thread myThread1 = new Thread(Schedule);
            myThread1.IsBackground = true;
            myThread1.Start();

            // Run an additional thread DBScanner
            Thread myThread2 = new Thread(DBScanner);
            myThread2.IsBackground = true;
            myThread2.Start();

            // Run an additional thread SCLogScanner
            Thread myThread3 = new Thread(SCLogScanner);
            myThread3.IsBackground = true;
            myThread3.Start();

            // Run an additional thread SendingAllNickname
            Thread myThread4 = new Thread(SendingAllNickname);
            myThread4.IsBackground = true;
            myThread4.Start();

            AddLog("Start " + progName);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, null);
            }
        }
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button3_Click(sender, null);
            }
        }
        private void button1_Click(object sender, EventArgs e) // Add new nickname
        {
            string nickname = textBox1.Text;

            SC sc = GetDataFromSC(nickname);
            sc.date = DateTime.Now;
            if (sc.code != 0)
            {
                AddLog(sc.text, true);
                return;
            }

            NicknameUid nicknameUid = new NicknameUid();
            nicknameUid.nickname = sc.data.nickname;
            nicknameUid.uid = sc.data.uid;

            if (nicknameBase.Contains(nicknameUid))
            {
                AddLog("Nickname '" + nickname + "' already exists in the database.", true);
                return;
            }

            nicknameBase.Add(nicknameUid);
            dataBase.Add(sc);

            SaveDataToFile();
            UpdateTable1();

            AddLog("Nickname '" + nickname + "' added.");
        }
        private void button2_Click(object sender, EventArgs e) // Delete nickname
        {
            NicknameUid nicknameUid = new NicknameUid();
            nicknameUid.nickname = textBox1.Text;

            if (nicknameBase.Contains(nicknameUid))
            {
                nicknameBase.Remove(nicknameUid);
                AddLog("Nickname '" + nicknameUid.nickname + "' removed.");

                SC sc = new SC();
                sc.code = 2;
                sc.data = new SCdata();
                sc.data.nickname = nicknameUid.nickname;
                while (dataBase.Contains(sc))
                {
                    dataBase.Remove(sc);
                }
            }
            else
            {
                AddLog("Nickname '" + nicknameUid.nickname + "' is not in the database.", true);
            }

            SaveDataToFile();
            UpdateTable1();
        }

        private void AddLog(string inputText, bool isShowMessageBox = false)
        {
            DateTime localDate = DateTime.Now;
            string logText = localDate.ToString(" [HH:mm:ss.fff] ") + inputText;
            try
            {
                BeginInvoke(new MethodInvoker(delegate { listBox1.Items.Add(logText); }));
                BeginInvoke(new MethodInvoker(delegate { listBox1.SelectedIndex = listBox1.Items.Count - 1; }));
            }
            catch
            {
                listBox1.Items.Add(logText);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }
            for (int i = 0; i < 5; i++ )
            {
                try
                {
                    File.AppendAllText(myPath + progName + ".log", logText + "\r\n");
                    break;
                }
                catch
                {
                    Thread.Sleep(10);
                }
            }
            
            
            if (isShowMessageBox)
            {
                MessageBox.Show(inputText);
            }
        }
        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        private bool IsSetup()
        {
            string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string goodPath = ProgramFiles + "\\" + "scorpclub" + "\\" + progName + "\\";
            if (myPath == goodPath)
            {
                return true;
            }
            return false;
        }
        private void ProcessStartedByAdmin(string arguments = null)
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);
            if (hasAdministrativeRight == false)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(); //создаем новый процесс
                processInfo.Verb = "runas"; //в данном случае указываем, что процесс должен быть запущен с правами администратора
                processInfo.FileName = Assembly.GetExecutingAssembly().Location; //указываем исполняемый файл (программу) для запуска
                processInfo.Arguments = arguments;
                try
                {
                    Process.Start(processInfo); //пытаемся запустить процесс
                    Environment.Exit(0);
                }
                catch { }
            }
        }
        private SC GetDataFromSC(string nickname)
        {
            WebClient client = new WebClient();
            string buffer = client.DownloadString("http://gmt.star-conflict.com/pubapi/v1/userinfo.php?nickname=" + nickname);
            SC sc = JsonConvert.DeserializeObject<SC>(buffer);

            if (sc.code == 1)
            {
                return sc;
            }

            if (sc.data.clan == null)
            {
                sc.data.clan = new SCdataClan();
                sc.data.clan.name = "-------------------";
                sc.data.clan.tag = "-----";
            }
            if (sc.data.clan.tag == null)
            {
                sc.data.clan.tag = "-----";
            }
            if (sc.data.pvp == null || sc.data.pvp.gamePlayed == null || sc.data.pvp.gameWin == null || sc.data.pvp.totalAssists == null
                || sc.data.pvp.totalBattleTime == null || sc.data.pvp.totalDeath == null || sc.data.pvp.totalDmgDone == null
                || sc.data.pvp.totalHealingDone == null || sc.data.pvp.totalKill == null || sc.data.pvp.totalVpDmgDone == null)
            {
                sc.data.pvp = new SCdataPvp();
                sc.data.pvp.gamePlayed = 0;
                sc.data.pvp.gameWin = 0;
                sc.data.pvp.totalAssists = 0;
                sc.data.pvp.totalBattleTime = 0;
                sc.data.pvp.totalDmgDone = 0;
                sc.data.pvp.totalHealingDone = 0;
                sc.data.pvp.totalKill = 0;
                sc.data.pvp.totalVpDmgDone = 0;
            }

            return sc;
        }

        private void UpdateTable1()
        {
            if (File.Exists(myPath + "nicknameuid.data"))
            {
                nicknameBase = BytesToListNicknameUid(File.ReadAllBytes(myPath + "nicknameuid.data"));
            }
            if (File.Exists(myPath + "schistory.data"))
            {
                dataBase = BytesToListSC(File.ReadAllBytes(myPath + "schistory.data"));
            }

            // display option
            dataGridView1.Rows.Clear();
            foreach (NicknameUid item in nicknameBase)
            {
                List<SC> buffer = dataBase.FindAll(x => x.data.uid.Equals(item.uid));
                //buffer.Sort();

                List<object> row = new List<object>();
                row.Add(item.uid);
                row.Add(item.nickname);
                row.Add(buffer[0].data.clan.name);
                row.Add(buffer[0].data.clan.tag);

                SC oldSc = new SC();
                if (buffer.Count > 1)
                {
                    oldSc = buffer[1];
                }
                
                row = ShowColumns(row, buffer[0], oldSc);
                dataGridView1.Rows.Add(row.ToArray());
            }
        }
        private List<object> ShowColumns(List<object> row, SC sc, SC oldSc)
        {
            foreach (string column in mySettings.columnsList)
            {
                if (column == "effRating")
                {
                    row.Add(sc.data.effRating);
                }
                if (column == "karma")
                {
                    row.Add(sc.data.karma);
                }
                if (column == "prestigeBonus")
                {
                    row.Add(sc.data.prestigeBonus);
                }
                if (column == "gamePlayed")
                {
                    row.Add(sc.data.pvp.gamePlayed);
                }
                if (column == "gameWin")
                {
                    row.Add(sc.data.pvp.gameWin);
                }
                if (column == "totalAssists")
                {
                    row.Add(sc.data.pvp.totalAssists);
                }
                if (column == "totalBattleTime")
                {
                    row.Add(sc.data.pvp.totalBattleTime);
                }
                if (column == "totalDeath")
                {
                    row.Add(sc.data.pvp.totalDeath);
                }
                if (column == "totalDmgDone")
                {
                    row.Add(sc.data.pvp.totalDmgDone);
                }
                if (column == "totalHealingDone")
                {
                    row.Add(sc.data.pvp.totalHealingDone);
                }
                if (column == "totalKill")
                {
                    row.Add(sc.data.pvp.totalKill);
                }
                if (column == "totalVpDmgDone")
                {
                    row.Add(sc.data.pvp.totalVpDmgDone);
                }

                if (column == "K/D")
                {
                    double totalKill = sc.data.pvp.totalKill;
                    double totalDeath = sc.data.pvp.totalDeath;
                    double result = Math.Round(totalKill / totalDeath, 2);
                    if (totalDeath == 0) { result = 0; }
                    row.Add(result);
                }
                if (column == "KDA")
                {
                    double totalKillAssists = sc.data.pvp.totalKill + sc.data.pvp.totalAssists;
                    double totalDeath = sc.data.pvp.totalDeath;
                    double result = Math.Round(totalKillAssists / totalDeath, 2);
                    if (totalDeath == 0) { result = 0; }
                    row.Add(result);
                }
                if (column == "WinRate")
                {
                    double gameWin = sc.data.pvp.gameWin;
                    double gamePlayed = sc.data.pvp.gamePlayed;
                    double result = Math.Round(gameWin / gamePlayed, 2);
                    if (gamePlayed == 0) { result = 0; }
                    row.Add(result * 100 + "%");
                }
                if (column == "W/L")
                {
                    double gameWin = sc.data.pvp.gameWin;
                    double gameLose = sc.data.pvp.gamePlayed - sc.data.pvp.gameWin;
                    double result = Math.Round(gameWin / gameLose, 2);
                    if (gameLose == 0) { result = 0; }
                    row.Add(result);
                }
                if (column == "K/D+" && oldSc.data != null)
                {
                    double totalKillLastDay = sc.data.pvp.totalKill - oldSc.data.pvp.totalKill;
                    double totalDeathLastDay = sc.data.pvp.totalDeath - oldSc.data.pvp.totalDeath;
                    double result = Math.Round(totalKillLastDay / totalDeathLastDay, 2);
                    if (totalDeathLastDay == 0) { result = 0; }
                    row.Add(result);
                }
                else if (column == "K/D+" && oldSc.data == null)
                {
                    row.Add(0.0);
                }
                if (column == "KDA+" && oldSc.data != null)
                {
                    double totalKillAssistsLastDay = (sc.data.pvp.totalKill + sc.data.pvp.totalAssists) - (oldSc.data.pvp.totalKill + oldSc.data.pvp.totalAssists);
                    double totalDeathLastDay = sc.data.pvp.totalDeath - oldSc.data.pvp.totalDeath;
                    double result = Math.Round(totalKillAssistsLastDay / totalDeathLastDay, 2);
                    if (totalDeathLastDay == 0) { result = 0; }
                    row.Add(result);
                }
                else if (column == "KDA+" && oldSc.data == null)
                {
                    row.Add(0.0);
                }
                if (column == "WinRate+" && oldSc.data != null)
                {
                    double gameWinLastDay = sc.data.pvp.gameWin - oldSc.data.pvp.gameWin;
                    double gamePlayedLastDay = sc.data.pvp.gamePlayed - oldSc.data.pvp.gamePlayed;
                    double result = Math.Round(gameWinLastDay / gamePlayedLastDay, 2);
                    if (gamePlayedLastDay == 0) { result = 0; }
                    row.Add(result * 100 + "%");
                }
                else if (column == "WinRate+" && oldSc.data == null)
                {
                    row.Add("0%");
                }
                if (column == "W/L+" && oldSc.data != null)
                {
                    double gameWinLastDay = sc.data.pvp.gameWin - oldSc.data.pvp.gameWin;
                    double gameLoseLastDay = (sc.data.pvp.gamePlayed - sc.data.pvp.gameWin) - (oldSc.data.pvp.gamePlayed - oldSc.data.pvp.gameWin);
                    double result = Math.Round(gameWinLastDay / gameLoseLastDay, 2);
                    if (gameLoseLastDay == 0) { result = 0; }
                    row.Add(result);
                }
                else if (column == "W/L+" && oldSc.data == null)
                {
                    row.Add(0.0);
                }
                if (column == "gamePlayed+" && oldSc.data != null)
                {
                    Int64 result = sc.data.pvp.gamePlayed - oldSc.data.pvp.gamePlayed;
                    row.Add(result);
                }
                else if (column == "gamePlayed+" && oldSc.data == null)
                {
                    row.Add((Int64)0);
                }
            }
            return row;
        }

        private void UpdateVisibleSettings()
        {
            if (IsSetup() == false)
            {
                checkBox2.Enabled = false;
                label2.Visible = true;
                button5.Visible = true;
            }
            if (checkBox2.Checked == false)
            {
                checkBox3.Enabled = false;
                label4.Visible = true;
            }
            else
            {
                checkBox3.Enabled = true;
                label4.Visible = false;
            }
            if (checkBox3.Checked == false || checkBox2.Checked == false)
            {
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
            }
            else
            {
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
            }
        }
        private void SaveDataToFile()
        {
            File.WriteAllBytes(myPath + "nicknameuid.data", NicknameUidListToBytes(nicknameBase));
            File.WriteAllBytes(myPath + "schistory.data", SCListToBytes(dataBase));
        }
        private byte[] SCListToBytes(List<SC> inputObject)
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            MemoryStream mStream = new MemoryStream();

            binFormatter.Serialize(mStream, inputObject);

            return mStream.ToArray();
        }
        private byte[] NicknameUidListToBytes(List<NicknameUid> inputObject)
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            MemoryStream mStream = new MemoryStream();

            binFormatter.Serialize(mStream, inputObject);

            return mStream.ToArray();
        }
        private List<SC> BytesToListSC(byte[] objectBytes)
        {
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();

            // Where 'objectBytes' is your byte array.
            mStream.Write(objectBytes, 0, objectBytes.Length);
            mStream.Position = 0;

            return binFormatter.Deserialize(mStream) as List<SC>;
        }
        private List<NicknameUid> BytesToListNicknameUid(byte[] objectBytes)
        {
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();

            // Where 'objectBytes' is your byte array.
            mStream.Write(objectBytes, 0, objectBytes.Length);
            mStream.Position = 0;

            return binFormatter.Deserialize(mStream) as List<NicknameUid>;
        }

        private void button3_Click(object sender, EventArgs e) // Find nickname
        {
            string nickname = textBox2.Text;
            if (nickname == "")
            {
                dataGridView2.Rows.Clear();
                return;
            }

            // Update table2
            SC oldSc = new SC();
            List<SC> buffer = dataBase.FindAll(x => x.data.nickname.Contains(nickname));
            buffer.Sort();

            dataGridView2.Rows.Clear();
            foreach (SC sc in buffer)
            {
                List<object> row = new List<object>();
                row.Add(sc.date.AddDays(-1).ToString("yyyy-MM-dd"));
                row.Add(sc.data.uid);
                row.Add(sc.data.nickname);
                row.Add(sc.data.clan.name);
                row.Add(sc.data.clan.tag);

                row = ShowColumns(row, sc, oldSc);
                oldSc = sc;

                dataGridView2.Rows.Add(row.ToArray());
            }
        }
        private void button4_Click(object sender, EventArgs e) // Update all
        {
            button4.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            AddLog("Start all updating history.");

            foreach (NicknameUid item in nicknameBase)
            {
                if (checkedListBox3.GetItemChecked(1)) // if data source are schistory
                {
                    DownloadDataFromHistory(item);
                }
                else
                {
                    DownloadDataFromSC(item);
                }
            }
            SaveDataToFile();
            button3_Click(sender, e);

            AddLog("End all updating history.");
            button4.Enabled = true;
            this.Cursor = Cursors.Default;
        }
        private void DownloadDataFromSC(NicknameUid input)
        {
            SC sc = GetDataFromSC(input.nickname);
            sc.date = DateTime.Now;
            if (sc.code != 0)
            {
                AddLog(sc.text);
                return;
            }

            // Delete today result
            foreach (SC item2 in dataBase.ToArray())
            {
                if (item2.data.uid == sc.data.uid && item2.date.Year == sc.date.Year && item2.date.Month == sc.date.Month && item2.date.Day == sc.date.Day)
                {
                    dataBase.Remove(sc);
                }
            }
            dataBase.Add(sc);
        }
        private void DownloadDataFromHistory(NicknameUid input)
        {
            WebClient client = new WebClient();
            string buffer = client.DownloadString("http://schistory.space/api/v1/userinfo.php?nickname=" + input.nickname);
            Space space = JsonConvert.DeserializeObject<Space>(buffer);

            if (space.result < 0)
            {
                AddLog(space.text, true);
            }
            else
            {
                //space.bigdata.Sort();
                foreach (var item in space.bigdata)
                {
                    SC sc = new SC();
                    sc.data = new SCdata();
                    sc.data.clan = new SCdataClan();
                    sc.data.pvp = new SCdataPvp();

                    sc.code = 0;
                    sc.result = "ok";
                    sc.date = item.date.AddDays(1);
                    sc.data.effRating = item.effRating;
                    sc.data.karma = item.karma;
                    sc.data.prestigeBonus = item.prestigeBonus;
                    sc.data.nickname = item.nickname;
                    sc.data.uid = item.uid;
                    sc.data.pvp.gamePlayed = item.gamePlayed;
                    sc.data.pvp.gameWin = item.gameWin;
                    sc.data.pvp.totalAssists = item.totalAssists;
                    sc.data.pvp.totalBattleTime = item.totalBattleTime;
                    sc.data.pvp.totalDeath = item.totalDeath;
                    sc.data.pvp.totalDmgDone = item.totalDmgDone;
                    sc.data.pvp.totalHealingDone = item.totalHealingDone;
                    sc.data.pvp.totalKill = item.totalKill;
                    sc.data.pvp.totalVpDmgDone = item.totalVpDmgDone;
                    sc.data.clan.name = item.clanName;
                    sc.data.clan.tag = item.clanTag;

                    // Delete existing values
                    foreach (SC item2 in dataBase.ToArray())
                    {
                        if (item2.data.uid == sc.data.uid && item2.date.Year == sc.date.Year && item2.date.Month == sc.date.Month && item2.date.Day == sc.date.Day)
                        {
                            dataBase.Remove(sc);
                        }
                    }

                    dataBase.Add(sc);
                }
            }
        }
        private void button5_Click(object sender, EventArgs e) // Install the program
        {
            ProcessStartedByAdmin("-i");
        }

        private void checkBox2_CheckStateChanged(object sender, EventArgs e) // Run with system
        {
            mySettings.runWithSystem = checkBox2.Checked;
            UpdateVisibleSettings();

            if (checkBox2.Checked)
            {
                // Прописываем себя в автозагрузку
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string path = programFiles + "\\" + "scorpclub" + "\\" + progName + "\\";
                string Location = Assembly.GetExecutingAssembly().Location;
                string exeName = Location.Substring(Location.LastIndexOf("\\") + 1);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",  // Путь к приложению
                    Arguments = "/Create /SC ONLOGON /TN " + progName + " /TR " + '"' + path + exeName + " -m" + '"' + " /RL HIGHEST /F", // Передача Аргументов
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
            }
            else
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",  // Путь к приложению
                        Arguments = "/Delete /TN " + progName + " /F", // Передача Аргументов
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(startInfo);
                }
                catch (Exception err)
                {
                    AddLog(err.Message);
                }
            }
        }
        private void checkBox3_CheckStateChanged(object sender, EventArgs e) // Automatically update the history on a schedule
        {
            UpdateVisibleSettings();
            mySettings.autoUpdateHistory = checkBox3.Checked;
        }

        private void Schedule()
        {
            bool doing = false;
            DateTime localDate;
            while (true)
            {
                Thread.Sleep(10000);
                if (checkBox3.Checked == false)
                {
                    continue;
                }
                localDate = DateTime.Now;
                if (numericUpDown1.Value == localDate.Hour && numericUpDown2.Value == localDate.Minute && doing == false)
                {
                    doing = true;
                    button4_Click(null, null);
                }
                if (numericUpDown1.Value == localDate.Hour && numericUpDown2.Value + 1 == localDate.Minute && doing == true)
                {
                    doing = false;
                }
            }
        }
        private void DBScanner()
        {
            while (true)
            {
                Thread.Sleep(60000);
                List<NicknameUid> buffer = nicknameBase;
                foreach (NicknameUid item in buffer)
                {
                    if (nicknameBigList.Contains(item.nickname) == false && saveNicknameBigList.Contains(item.nickname) == false)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (!isWorkinWithNicknameBigString)
                            {
                                nicknameBigList.Add(item.nickname);
                                break;
                            }
                            else
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
            }
        }
        private void SCLogScanner()
        {
            string gamelogs_route = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\StarConflict\logs";
            cycle = log_cycle = 1;

            while (true)
            {
                // Проверка запущенной игры
                bool sc_run = IsStarConflictRun();

                // Определение актуальных логов
                List<string> dirs = new List<string>(Directory.EnumerateDirectories(gamelogs_route));
                string good_log = dirs[dirs.Count - 1] + @"\";

                // Чтение логов
                if (File.Exists(good_log + "combat.log") & sc_run)
                {
                    LogReader(good_log);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
        private bool IsStarConflictRun()
        {
            bool sc_run = false;
            Process[] sc_process = Process.GetProcessesByName("game");
            if (sc_process.Length > 0)
            {
                sc_run = true;
            }
            return sc_run;
        }
        private void LogReader(string good_log)
        {
            if (log_cycle == 1)
            {
                good_log_old = good_log;
                gamelog_file = new FileStream(good_log + "chat.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite); //создаем файловый поток
                gamelog_reader = new StreamReader(gamelog_file); // создаем «потоковый читатель» и связываем его с файловым потоком
                log_cycle++;
            }
            if (good_log != good_log_old)
            {
                AddLog("Change log sub-directory: " + good_log);
                good_log_old = good_log;
                gamelog_file = new FileStream(good_log + "chat.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite); //создаем файловый поток
                gamelog_reader = new StreamReader(gamelog_file); // создаем «потоковый читатель» и связываем его с файловым потоком
            }

            string inputgamelog = gamelog_reader.ReadLine();
            string gamelog_item = inputgamelog ?? "";

            if (gamelog_item.Length > 0)
            {
                if (gamelog_item.IndexOf("CHAT") > -1)
                {
                    LogParsing(gamelog_item);
                }
            }
            else
            {
                Thread.Sleep(700);
            }
            cycle = cycle + 1;
        }
        private void LogParsing(string inputText)
        {
            if (inputText.IndexOf('[') > -1)
            {
                string nickname = inputText.Remove(0, inputText.IndexOf('[') + 1);
                nickname = nickname.Remove(nickname.IndexOf(']'));
                nickname = nickname.Trim();

                string chat = inputText.Remove(0, inputText.IndexOf('<') + 1);
                chat = chat.Remove(chat.IndexOf('>'));
                chat = chat.Trim();
                if (chat.Contains("general"))
                {
                    chat = "general";
                }
                else if (chat.Contains("clan"))
                {
                    chat = "clan";
                }
                else if (chat.Contains("PRIVATE"))
                {
                    chat = "private";
                }

                // Add in chat lsit
                NicknameFromChat nicknameFromChat = new NicknameFromChat();
                nicknameFromChat.nickname = nickname;
                nicknameFromChat.chat = chat;

                if (nicknameListFromChat.Contains(nicknameFromChat) == false)
                {
                    nicknameListFromChat.Add(nicknameFromChat);
                }

                if (nickname.Length == 0 || nicknameBigList.Contains(nickname) || saveNicknameBigList.Contains(nickname))
                {
                    return;
                }

                // Add in save list
                for (int i = 0; i < 5; i++ )
                {
                    if (!isWorkinWithNicknameBigString)
                    {
                        //AddLog("Add nickname: " + nickname);
                        nicknameBigList.Add(nickname);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
        }

        private void SendingAllNickname()
        {
            while (true)
            {
                Thread.Sleep(3600000);
                if (nicknameBigList.Count > 0)
                {
                    isWorkinWithNicknameBigString = true;
                    saveNicknameBigList.AddRange(nicknameBigList);
                    string[] buffer = nicknameBigList.ToArray();
                    nicknameBigList.Clear();
                    InfoSend(buffer);
                    isWorkinWithNicknameBigString = false;
                }
            }
        }
        private void InfoSend(string[] inputArray)
        {
            TcpClient tcpClient = null;
            NetworkStream stream = null;
            try
            {
                //AddLog("Connect to host: " + host);
                if (inputArray.Length == 0) { return; }
                tcpClient = new TcpClient(host, port);
                stream = tcpClient.GetStream();
                foreach (var item in inputArray)
                {
                    if (item.Length > 0)
                    {
                        //AddLog("Send nickname: " + item);
                        string message = "<nickname>" + item + "</nickname>";
                        TcpSend(message, stream);
                    }
                }
                stream.Close();
                tcpClient.Close();
                //AddLog("Close connection.");
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
        private string TcpSend(string sendText, NetworkStream stream)
        {
            byte[] buffer = new byte[2048];
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
            stream.Write(data, 0, data.Length);
            //int bytes = stream.Read(buffer, 0, buffer.Length);
            //Decoder decoder = Encoding.UTF8.GetDecoder();
            //char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];

            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);
            string outputText = messageData.ToString();
            if (outputText.IndexOf("<EOF>") > -1) { outputText = outputText.Remove(outputText.IndexOf("<EOF>")); }
            return outputText;
        }

        private void SaveSettings()
        {
            File.WriteAllBytes(myPath + "settings.data", ProgrammSettingsToBytes(mySettings));
        }
        private byte[] ProgrammSettingsToBytes(ProgramSettings inputObject)
        {
            BinaryFormatter binFormatter = new BinaryFormatter();
            MemoryStream mStream = new MemoryStream();

            binFormatter.Serialize(mStream, inputObject);

            return mStream.ToArray();
        }
        private ProgramSettings BytesToProgramSettings(byte[] objectBytes)
        {
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();

            // Where 'objectBytes' is your byte array.
            mStream.Write(objectBytes, 0, objectBytes.Length);
            mStream.Position = 0;

            return binFormatter.Deserialize(mStream) as ProgramSettings;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) // Exiting
        {
            SaveSettings();
            if (isClose == false)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            mySettings.hours = Convert.ToInt32(numericUpDown1.Value);
        }
        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            mySettings.hours = Convert.ToInt32(numericUpDown2.Value);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowMe();
            tabControl1.SelectedIndex = 0;
        }
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ShowMe();
            tabControl1.SelectedIndex = 1;
        }
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ShowMe();
            tabControl1.SelectedIndex = 2;
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ShowMe();
            tabControl1.SelectedIndex = 3;
        }
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            ShowMe();
            tabControl1.SelectedIndex = 4;
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            isClose = true;
            this.Close();
        }
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            ShowMe();
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowMe();
        }
        private void ShowMe()
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
            }
            this.Show();
        }

        private void UpdateNicknamesManagement(object sender, EventArgs e)
        {
            button9.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            listView1.Items.Clear();
            listView2.Items.Clear();
            foreach (NicknameUid item in nicknameBase)
            {
                ListViewItem lvi = new ListViewItem(new string[] { item.nickname, dataBase.Find(x => x.data.nickname.Contains(item.nickname)).data.clan.tag });
                listView2.Items.Add(lvi);
            }

            //
            nicknameListFromChat.Sort();
            foreach (NicknameFromChat item in nicknameListFromChat)
            {
                if (ignorNicknameBigList.Contains(item.nickname) || listView2.FindItemWithText(item.nickname) != null)
                {
                    continue;
                }

                Color color = Color.Black;
                if (item.chat == "general")
                {
                    if (checkBox6.Checked == false)
                    {
                        continue;
                    }
                    color = Color.FromArgb(64, 64, 64);
                }
                else if (item.chat == "clan")
                {
                    if (checkBox7.Checked == false)
                    {
                        continue;
                    }
                    color = Color.DarkGreen;
                }
                else if (item.chat == "private")
                {
                    if (checkBox8.Checked == false)
                    {
                        continue;
                    }
                    color = Color.Purple;
                }
                else
                {
                    continue;
                }

                // Check nickname
                SC sc = GetDataFromSC(item.nickname);
                if (sc.code == 1)
                {
                    continue;
                }

                // Add nickname to listView1
                ListViewItem lvItem = new ListViewItem(new string[] { item.nickname, sc.data.clan.tag });
                lvItem.ForeColor = color;
                listView1.Items.Add(lvItem);

            }

            label8.Text = nicknameBase.Count.ToString();

            button9.Enabled = true;
            this.Cursor = Cursors.Default;
        }

        private void UpdateDataGridView1Columns()
        {
            if (mySettings.columnsList == null)
            {
                mySettings.columnsList = new List<string>();
            }

            dataGridView2.Columns.Clear();

            dataGridView1.ColumnCount = mySettings.columnsList.Count + 4;
            dataGridView1.Columns[0].Name = "uid";
            dataGridView1.Columns[1].Name = "nickname";
            dataGridView1.Columns[2].Name = "clanName";
            dataGridView1.Columns[3].Name = "clanTag";

            int i = 4;
            foreach (string item in mySettings.columnsList)
            {
                dataGridView1.Columns[i].Name = item.ToString();
                i++;
            }
        }
        private void UpdateDataGridView2Columns()
        {
            if (mySettings.columnsList == null)
            {
                mySettings.columnsList = new List<string>();
            }

            dataGridView2.Columns.Clear();

            dataGridView2.ColumnCount = mySettings.columnsList.Count + 5;
            dataGridView2.Columns[0].Name = "date";
            dataGridView2.Columns[1].Name = "uid";
            dataGridView2.Columns[2].Name = "nickname";
            dataGridView2.Columns[3].Name = "clanName";
            dataGridView2.Columns[4].Name = "clanTag";

            int i = 5;
            foreach (string item in mySettings.columnsList)
            {
                dataGridView2.Columns[i].Name = item.ToString();
                i++;
            }
        }
        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked && mySettings.columnsList.Contains(checkedListBox1.Items[e.Index].ToString()) == false)
            {
                mySettings.columnsList.Add(checkedListBox1.Items[e.Index].ToString());
            }
            else if (e.NewValue == CheckState.Unchecked && mySettings.columnsList.Contains(checkedListBox1.Items[e.Index].ToString()) == true)
            {
                mySettings.columnsList.Remove(checkedListBox1.Items[e.Index].ToString());
            }

            SaveSettings();

            UpdateDataGridView1Columns();
            UpdateDataGridView2Columns();
            UpdateTable1();
            button3_Click(sender, e); // Update table2
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e) // Select all
        {
            foreach (ListViewItem item in listView1.Items)
            {
                item.Checked = checkBox9.Checked;
            }
        }
        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.Items)
            {
                item.Checked = checkBox10.Checked;
            }
        }

        private void button7_Click(object sender, EventArgs e) // Add nickname from dataGridView
        {
            button7.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            foreach (ListViewItem item in listView1.CheckedItems)
            {
                item.Checked = false;
                listView1.Items.Remove(item);
                listView2.Items.Add(item);

                textBox1.Text = item.Text;
                button1_Click(sender, e);
                textBox1.Text = null;
            }

            button7.Enabled = true;
            this.Cursor = Cursors.Default;
        }
        private void button8_Click(object sender, EventArgs e) // Delete nickname from dataGridView
        {
            button8.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            foreach (ListViewItem item in listView2.CheckedItems)
            {
                item.Checked = false;
                listView2.Items.Remove(item);
                listView1.Items.Add(item);

                textBox1.Text = item.Text;
                button2_Click(sender, e);
                textBox1.Text = null;
            }

            button8.Enabled = true;
            this.Cursor = Cursors.Default;
        }
        private void button10_Click(object sender, EventArgs e) // Ignore nickname from dataGridView
        {
            button10.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            foreach (ListViewItem item in listView1.CheckedItems)
            {
                ignorNicknameBigList.Add(item.Text);
                listView1.Items.Remove(item);
            }

            button10.Enabled = true;
            this.Cursor = Cursors.Default;
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e) // Click uid from table1
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            tabControl1.SelectedIndex = 1;
            textBox2.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
            button3_Click(sender, null);
        }

        private void button6_Click(object sender, EventArgs e) // Import nicknames
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = openFileDialog1.OpenFile()) != null)
                {
                    using (myStream)
                    {
                        // Insert code to read the stream here.
                        StreamReader myReader = new StreamReader(myStream);
                        string inputString = myReader.ReadToEnd();
                        string[] inputArray = inputString.Split(new char[] { ' ', '\0', '\a', '\b', '\t', '\n', '\v', '\f', '\r', ',', '.', '|', '+', '-', '_' });

                        foreach (string nickname in inputArray)
                        {
                            // Check nickname length
                            if (nickname.Length == 0)
                            {
                                continue;
                            }

                            // Check nickname
                            if (GetDataFromSC(nickname).code == 1)
                            {
                                AddLog("Bad nickname from input file: " + nickname);
                                continue;
                            }

                            // Add nickname
                            AddLog("Add nickname from input file: " + nickname);
                            textBox1.Text = nickname;
                            button1_Click(sender, e);
                        }


                    }
                }
            }
        }

        private void button11_Click(object sender, EventArgs e) // Refresh modules list
        {
            checkedListBox2.Items.Clear();
            foreach (string item in Directory.GetFiles(myPath + "modules\\"))
            {
                checkedListBox2.Items.Add(item.Substring(item.LastIndexOf("\\") + 1));
            }
        }
        private void button12_Click(object sender, EventArgs e) // Add new module
        {
            string fileName;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((fileName = openFileDialog1.FileName) != null)
                {
                    string safeFileName = openFileDialog1.SafeFileName;
                    string destFileName = myPath + "modules\\" + safeFileName;
                    AddLog(fileName + " --> " + destFileName, true);
                    File.Copy(fileName, destFileName);

                    // Refresh modules list
                    button11_Click(sender, e);
                }
            }
        }
        private void button13_Click(object sender, EventArgs e) // Delete module
        {
            foreach (var item in checkedListBox2.CheckedItems)
            {
                string modul = item.ToString();
                File.Delete(myPath + "modules\\" + modul);
            }

            // Refresh modules list
            button11_Click(sender, e);
        }
        private void button14_Click(object sender, EventArgs e) // Start modul
        {
            foreach (var item in checkedListBox2.CheckedItems)
            {
                string modul = item.ToString();
                RunInSandbox(myPath + "modules\\" + modul, "schistory.Class1", "Main");
            }
        }
        private void RunInSandbox(string untrustedAssembly, string untrustedClass, string entryPoint)
        {
            try
            {
                Sandboxer aaa = new Sandboxer();
                aaa.pathToUntrusted = Path.GetTempPath() + "schistorysandboxer\\";
                //aaa.pathToUntrusted = untrustedAssembly.Remove(untrustedAssembly.LastIndexOf('\\'));
                aaa.untrustedAssembly = untrustedAssembly;
                aaa.untrustedClass = untrustedClass;
                aaa.entryPoint = entryPoint;

                object[] output = { aaa.pathToUntrusted, NicknameBaseToObject(nicknameBase), DataBaseToObject(dataBase) };
                aaa.parameters = new object[] { output };

                aaa.Main();
            }
            catch (Exception err)
            {
                AddLog("Can not run in Sandbox. Error message: " + err.ToString(), true);
            }
        }
        private object[] NicknameBaseToObject(List<NicknameUid> nicknameList)
        {
            int numb = 0;
            object[] output = new object[nicknameList.Count];
            foreach (NicknameUid item in nicknameList)
            {
                output[numb] = new object[] { item.nickname, item.uid };
                numb += 1;
            }
            return output;
        }
        private object[] DataBaseToObject(List<SC> scList)
        {
            int numb = 0;
            object[] output = new object[scList.Count];
            foreach (SC item in scList)
            {
                object result = item.result;
                object code = item.code;
                object text = item.text;
                object date = item.date;
                object effRating = item.data.effRating;
                object karma = item.data.karma;
                object nickname = item.data.nickname;
                object prestigeBonus = item.data.prestigeBonus;
                object uid = item.data.uid;
                object gamePlayed = item.data.pvp.gamePlayed;
                object gameWin = item.data.pvp.gameWin;
                object totalAssists = item.data.pvp.totalAssists;
                object totalBattleTime = item.data.pvp.totalBattleTime;
                object totalDeath = item.data.pvp.totalDeath;
                object totalDmgDone = item.data.pvp.totalDmgDone;
                object totalHealingDone = item.data.pvp.totalHealingDone;
                object totalKill = item.data.pvp.totalKill;
                object totalVpDmgDone = item.data.pvp.totalVpDmgDone;
                object clanName = item.data.clan.name;
                object clanTag = item.data.clan.tag;
                output[numb] = new object[] { result, code, text, date, effRating, karma, nickname, prestigeBonus, uid, gamePlayed, gameWin,
                    totalAssists, totalBattleTime, totalDeath, totalDmgDone, totalHealingDone, totalKill, totalVpDmgDone, clanName, clanTag };
                numb += 1;
            }
            return output;
        }

        private void button15_Click(object sender, EventArgs e) // Update only one nickname
        {
            button15.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            NicknameUid item = new NicknameUid();
            item.nickname = textBox2.Text;
            AddLog("Start update only this nickname: " + item.nickname);

            if (checkedListBox3.GetItemChecked(1)) // if data source are schistory
            {
                DownloadDataFromHistory(item);
            }
            else
            {
                DownloadDataFromSC(item);
            }
            button3_Click(sender, e);

            AddLog("End update.");
            button15.Enabled = true;
            this.Cursor = Cursors.Default;
        }

        private void checkedListBox3_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                for (int ix = 0; ix < checkedListBox3.Items.Count; ++ix)
                {
                    if (e.Index != ix) checkedListBox3.SetItemChecked(ix, false);
                }
            }

            if (e.NewValue == CheckState.Checked && mySettings.DataSourceList.Contains(checkedListBox3.Items[e.Index].ToString()) == false)
            {
                mySettings.DataSourceList.Add(checkedListBox3.Items[e.Index].ToString());
            }
            else if (e.NewValue == CheckState.Unchecked && mySettings.DataSourceList.Contains(checkedListBox3.Items[e.Index].ToString()) == true)
            {
                mySettings.DataSourceList.Remove(checkedListBox3.Items[e.Index].ToString());
            }
        }
    }

    [Serializable()]
    public class ProgramSettings
    {
        public bool runWithSystem { get; set; }
        public bool autoUpdateHistory { get; set; }
        public bool checkUpdates { get; set; }
        public bool autoUpdateProgramm { get; set; }
        public int hours { get; set; }
        public int minutes { get; set; }
        public List<string> columnsList { get; set; }
        public List<string> DataSourceList { get; set; }
    }

    [Serializable()]
    public class SC : IEquatable<SC>, IComparable<SC>
    {
        public string result { get; set; }
        public int code { get; set; }
        public string text { get; set; }
        public SCdata data { get; set; }
        public DateTime date { get; set; }

        public bool Equals(SC sc)
        {
            if (sc.code == 2 && this.data.nickname == sc.data.nickname)
            {
                return true;
            }
            else if (this.data.nickname == sc.data.nickname && this.date.Year == sc.date.Year && this.date.Month == sc.date.Month && this.date.Day == sc.date.Day)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public int CompareTo(SC p)
        {
            return this.date.CompareTo(p.date);
        }
    }
    [Serializable()]
    public class SCdata
    {
        public Int64 effRating { get; set; }
        public Int64 karma { get; set; }
        public string nickname { get; set; }
        public double prestigeBonus { get; set; }
        public Int64 uid { get; set; }
        public SCdataPvp pvp { get; set; }
        public SCdataClan clan { get; set; }
    }
    [Serializable()]
    public class SCdataPvp
    {
        public Int64 gamePlayed { get; set; }
        public Int64 gameWin { get; set; }
        public Int64 totalAssists { get; set; }
        public Int64 totalBattleTime { get; set; }
        public Int64 totalDeath { get; set; }
        public Int64 totalDmgDone { get; set; }
        public Int64 totalHealingDone { get; set; }
        public Int64 totalKill { get; set; }
        public Int64 totalVpDmgDone { get; set; }
    }
    [Serializable()]
    public class SCdataClan
    {
        public string name { get; set; }
        public string tag { get; set; }
    }
    [Serializable()]
    public class NicknameUid : IEquatable<NicknameUid>
    {
        public string nickname { get; set; }
        public Int64 uid { get; set; }

        public bool Equals(NicknameUid other)
        {
            if (this.nickname == other.nickname)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class NicknameFromChat : IEquatable<NicknameFromChat>, IComparable<NicknameFromChat>
    {
        public string nickname { get; set; }
        public string chat { get; set; }

        public bool Equals(NicknameFromChat nicknameFromChat)
        {
            if (nicknameFromChat.nickname == this.nickname && nicknameFromChat.chat == this.chat)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public int CompareTo(NicknameFromChat p)
        {
            return this.nickname.CompareTo(p.nickname);
        }
    }

    public class Space
    {
        public int result { get; set; }
        public string text { get; set; }
        public List<SpaceBigdata> bigdata { get; set; }
    }
    public class SpaceBigdata : IComparable<SpaceBigdata>
    {
        public DateTime date { get; set; }
        public Int64 uid { get; set; }
        public string nickname { get; set; }
        public Int64 effRating { get; set; }
        public Int64 karma { get; set; }
        public double prestigeBonus { get; set; }
        public Int64 gamePlayed { get; set; }
        public Int64 gameWin { get; set; }
        public Int64 totalAssists { get; set; }
        public Int64 totalBattleTime { get; set; }
        public Int64 totalDeath { get; set; }
        public Int64 totalDmgDone { get; set; }
        public Int64 totalHealingDone { get; set; }
        public Int64 totalKill { get; set; }
        public Int64 totalVpDmgDone { get; set; }
        public string clanName { get; set; }
        public string clanTag { get; set; }

        public int CompareTo(SpaceBigdata p)
        {
            return this.date.CompareTo(p.date);
        }
    }

    public class Sandboxer : MarshalByRefObject
    {
        public string pathToUntrusted { get; set; }
        public string untrustedAssembly { get; set; }
        public string untrustedClass { get; set; }
        public string entryPoint { get; set; }
        public Object[] parameters { get; set; }

        public void Main()
        {
            if (Directory.Exists(pathToUntrusted) == false)
            {
                Directory.CreateDirectory(pathToUntrusted);
            }
            //Setting the AppDomainSetup. It is very important to set the ApplicationBase to a folder   
            //other than the one in which the sandboxer resides.  
            AppDomainSetup adSetup = new AppDomainSetup();
            adSetup.ApplicationBase = Path.GetFullPath(pathToUntrusted);

            //Setting the permissions for the AppDomain. We give the permission to execute and to   
            //read/discover the location where the untrusted code is loaded.  
            PermissionSet permSet = new PermissionSet(PermissionState.None); //PermissionState.None
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution)); //SecurityPermissionFlag.Execution
            permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, Path.GetDirectoryName(untrustedAssembly)));
            permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess | FileIOPermissionAccess.PathDiscovery, Path.GetDirectoryName(pathToUntrusted)));
            permSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows));

            //We want the sandboxer assembly's strong name, so that we can add it to the full trust list.  
            StrongName fullTrustAssembly = typeof(Sandboxer).Assembly.Evidence.GetHostEvidence<StrongName>();

            //Now we have everything we need to create the AppDomain, so let's create it.  
            AppDomain newDomain = AppDomain.CreateDomain("Sandbox", null, adSetup, permSet, fullTrustAssembly);

            //Use CreateInstanceFrom to load an instance of the Sandboxer class into the  
            //new AppDomain.   
            ObjectHandle handle = Activator.CreateInstanceFrom(
                newDomain, typeof(Sandboxer).Assembly.ManifestModule.FullyQualifiedName,
                typeof(Sandboxer).FullName
                );
            //Unwrap the new domain instance into a reference in this domain and use it to execute the   
            //untrusted code.  
            Sandboxer newDomainInstance = (Sandboxer)handle.Unwrap();
            newDomainInstance.ExecuteUntrustedCode(untrustedAssembly, untrustedClass, entryPoint, parameters);
        }
        public void ExecuteUntrustedCode(string assemblyName, string typeName, string entryPoint, Object[] parameters)
        {
            //Load the MethodInfo for a method in the new Assembly. This might be a method you know, or   
            //you can use Assembly.EntryPoint to get to the main function in an executable.  
            MethodInfo target = Assembly.LoadFile(assemblyName).GetType(typeName).GetMethod(entryPoint);
            try
            {
                //Now invoke the method.  
                target.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                // When we print informations from a SecurityException extra information can be printed if we are   
                //calling it with a full-trust stack.  
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                MessageBox.Show("SecurityException caught:\n" + ex.ToString());
                CodeAccessPermission.RevertAssert();
            }
        }
    }  
}
