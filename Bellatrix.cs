using System;
using System.IO.Ports;
using System.Reflection;

namespace bellatrix
{
    public partial class Bellatrix : Form
    {
        private DataManager dataManager = new();
        private ConnectionManager connectionManager = new();

        private List<Command> LoadedCommands = new();
        private List<Script> LoadedScripts = new();
        private List<Command> LoadedScriptCommands = new();

        private List<Device> ConnectedDevices = new();

        public Bellatrix()
        {
            InitializeComponent();
        }

        // test devices
        private List<Device> fakedevices = new();

        private void Bellatrix_Load(object sender, EventArgs e)
        {
            // get version info for label
            BellatrixLabel.Text = $"Bellatrix (v{Assembly.GetEntryAssembly()?.GetName().Version})";

            // check to see if files exist, and create default files if they dont
            dataManager.FileCheck(this);

            // load files
            LoadedCommands = dataManager.LoadCommands();
            RefreshCommandsButton.PerformClick();
            LoadedScripts = dataManager.LoadScripts();
            RefreshScriptsButton.PerformClick();
        }

        private void RefreshDevicesButton_Click(object sender, EventArgs e)
        {
            ConsoleTextBox.AppendText("Bellatrix: Disconnecting current devices..." + Environment.NewLine);
            foreach (var item in ConnectedDevices)
            {
                if (item.PortConnection.IsOpen)
                {
                    item.PortConnection.Close();
                }
            }
            ConsoleTextBox.AppendText("Bellatrix: Refreshing devices..." + Environment.NewLine);
            ConnectedDevices = connectionManager.CollectDevices(this);
            DevicesDataGrid.DataSource = ConnectedDevices;
            DevicesDataGrid.Columns["PortName"].HeaderText = "Port";
            DevicesDataGrid.Columns["SerialNo"].HeaderText = "Serial No.";
            DevicesDataGrid.Columns["ModelNo"].HeaderText = "Model No.";
            DevicesDataGrid.Columns["ActivationLock"].HeaderText = "Activation Lock";
            DevicesDataGrid.Columns["StorageSize"].HeaderText = "Storage Size";
            DevicesDataGrid.Columns["NetworkLock"].HeaderText = "Network Lock";
            DevicesDataGrid.Columns["AndroidVersion"].HeaderText = "Android Version";

            DevicesDataGrid.Columns["PortConnection"].Visible = false;
            DevicesDataGrid.ClearSelection();
            ConsoleTextBox.AppendText("Bellatrix: Devices refreshed" + Environment.NewLine);
        }

        private void RefreshCommandsButton_Click(object sender, EventArgs e)
        {
            LoadedCommands = dataManager.LoadCommands();
            CommandsDataGrid.DataSource = LoadedCommands;
            CommandsDataGrid.Columns["Instruction"].HeaderText = "Command";
            CommandsDataGrid.Columns["Delay"].Visible = false;
            CommandsDataGrid.ClearSelection();
            CommandTextBox.Text = "";
            CommandDescTextBox.Text = "";
        }

        private void RefreshScriptsButton_Click(object sender, EventArgs e)
        {
            LoadedScripts = dataManager.LoadScripts();
            ScriptsDataGrid.DataSource = LoadedScripts;
            ScriptsDataGrid.Columns["Name"].HeaderText = "Script";
            ScriptsDataGrid.ClearSelection();
            LoadedScriptCommands.Clear();
            ScriptCommandsDataGrid.DataSource = LoadedScriptCommands;
            ScriptTextBox.Text = "";
            ScriptDescTextBox.Text = "";
        }

        private void CommandsDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            int rowindex = CommandsDataGrid.CurrentCell.RowIndex;
            foreach (var item in LoadedCommands)
            {
                if (item.Instruction == CommandsDataGrid["Instruction", rowindex].Value.ToString())
                {
                    CommandTextBox.Text = item.Instruction;
                    CommandDescTextBox.Text = item.Description;
                }
            }
        }

        private void ScriptsDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            int rowindex = ScriptsDataGrid.CurrentCell.RowIndex;

            foreach (var item in LoadedScripts)
            {
                if (item.Name == ScriptsDataGrid["Name", rowindex].Value.ToString())
                {
                    ScriptTextBox.Text = item.Name;
                    ScriptDescTextBox.Text = item.Description;
                    ScriptCommandsDataGrid.DataSource = item.Commands;
                    ScriptCommandsDataGrid.Columns["Instruction"].HeaderText = "Command";
                    ScriptCommandsDataGrid.Columns["Instruction"].FillWeight = 100;
                    ScriptCommandsDataGrid.Columns["Description"].FillWeight = 100;
                    ScriptCommandsDataGrid.Columns["Delay"].HeaderText = "Delay (ms)";
                    ScriptCommandsDataGrid.Columns["Delay"].FillWeight = 40;
                }
            }
        }

        private void SaveCommandButton_Click(object sender, EventArgs e)
        {
            Command command = new(CommandTextBox.Text, CommandDescTextBox.Text);
            dataManager.AddCommand(command);
            RefreshCommandsButton.PerformClick();
        }

        private void SaveScriptButton_Click(object sender, EventArgs e)
        {
            foreach (var item in LoadedScripts)
            {
                if (item.Name == ScriptTextBox.Text)
                {
                    Script script = new(ScriptTextBox.Text, ScriptDescTextBox.Text, item.Commands);
                    dataManager.AddScript(script);
                }
            }
            RefreshScriptsButton.PerformClick();
        }

        private void NewCommandButton_Click(object sender, EventArgs e)
        {
            CommandsDataGrid.ClearSelection();
            CommandTextBox.Text = "";
            CommandDescTextBox.Text = "";
        }

        private void NewScriptButton_Click(object sender, EventArgs e)
        {
            ScriptsDataGrid.ClearSelection();
            LoadedScriptCommands.Clear();
            ScriptCommandsDataGrid.DataSource = LoadedScriptCommands;
            ScriptTextBox.Text = "";
            ScriptDescTextBox.Text = "";
        }

        private void DeleteCommandButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this command?", "Delete Command", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                dataManager.RemoveCommand(CommandTextBox.Text);
                RefreshCommandsButton.PerformClick();
            }
        }

        private void DeleteScriptButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this script?", "Delete Script", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                dataManager.RemoveScript(ScriptTextBox.Text);
                RefreshScriptsButton.PerformClick();
            }
        }

        public static string ParseInformation(string data, string start, string end)
        {
            if (data.Contains(start) && data.Contains(end))
            {
                int Start, End;
                Start = data.IndexOf(start, 0) + start.Length;
                End = data.IndexOf(end, Start);
                return data.Substring(Start, End - Start);
            }
            return "";
        }

        internal void HandleResponse(object sender, SerialDataReceivedEventArgs e)
        {
            Device respondingdevice = new();

            foreach (Device device in ConnectedDevices)
            {
                if (device.PortConnection == (SerialPort)sender)
                {
                    respondingdevice = device;
                }
            }

            // need to add the storage size calculator

            string response = respondingdevice.PortConnection.ReadExisting();

            BeginInvoke(new Action(() =>
            {
                ConsoleTextBox.AppendText($"Device: {respondingdevice.PortName}" + Environment.NewLine + response);
            }));

            switch (response)
            {
                case string x when x.Contains("+DEVCONINFO"):
                    BeginInvoke(new Action(() =>
                    {
                        foreach (Device device in ConnectedDevices)
                        {
                            if (device.PortName == respondingdevice.PortName)
                            {
                                device.IMEI = ParseInformation(response, "IMEI(", ");");
                                device.SerialNo = ParseInformation(response, "SN(", ");");
                                device.Carrier = ParseInformation(response, "PRD(", ");");
                                device.ModelNo = ParseInformation(response, "MN(", ");");
                                DevicesDataGrid.Refresh();
                            }
                        }
                    }));
                    break;

                case string x when x.Contains("+REACTIVE"):
                    BeginInvoke(new Action(() =>
                    {
                        string activationstatus = ParseInformation(response, "\n+REACTIVE:1,", "\r\n");
                        bool activationlocked = false;
                        switch (activationstatus)
                        {
                            case "TRIGGERED":
                                activationlocked = true;
                                break;

                            case "LOCK":
                                activationlocked = true;
                                break;

                            case "NG(-2)":
                                activationlocked = false;
                                break;

                            case "UNLOCK":
                                activationlocked = false;
                                break;

                            default:
                                break;
                        }
                        foreach (Device device in ConnectedDevices)
                        {
                            if (device.PortName == respondingdevice.PortName)
                            {
                                device.ActivationLock = activationlocked.ToString();
                                DevicesDataGrid.Refresh();
                            }
                        }
                    }));
                    break;

                case string x when x.Contains("+VERSNAME"):
                    BeginInvoke(new Action(() =>
                    {
                        foreach (Device device in ConnectedDevices)
                        {
                            if (device.PortName == respondingdevice.PortName)
                            {
                                device.AndroidVersion = ParseInformation(response, "NAME:3,", "OK");
                                DevicesDataGrid.Refresh();
                            }
                        }
                    }));
                    break;

                case string x when x.Contains("+SVCIFPGM"):
                    BeginInvoke(new Action(() =>
                    {
                        string networklockstatus = ParseInformation(response, "4,", ",OK");
                        foreach (Device device in ConnectedDevices)
                        {
                            if (device.PortName == respondingdevice.PortName)
                            {
                                if (networklockstatus == "LOCK")
                                {
                                    device.NetworkLock = "True";
                                    DevicesDataGrid.Refresh();
                                }
                                else
                                {
                                    device.NetworkLock = "False";
                                    DevicesDataGrid.Refresh();
                                }
                            }
                        }
                    }));
                    break;

                default:
                    break;
            }
        }

        private void RunCommandButton_Click(object sender, EventArgs e)
        {
            // i know this is shitty, ill fix it later
            foreach (DataGridViewRow row in DevicesDataGrid.Rows)
            {
                if (row.Selected)
                {
                    foreach (Device device in ConnectedDevices)
                    {
                        if (row.Cells[0].Value.ToString() == device.PortName)
                        {
                            foreach (Command command in LoadedCommands)
                            {
                                if (command.Instruction == CommandTextBox.Text)
                                {
                                    connectionManager.RunCommand(device, command);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SendCommandButton_Click(object sender, EventArgs e)
        {
            Command command = new(CommandLineTextBox.Text, "");

            foreach (DataGridViewRow row in DevicesDataGrid.Rows)
            {
                if (row.Selected)
                {
                    foreach (Device device in ConnectedDevices)
                    {
                        if (row.Cells[0].Value.ToString() == device.PortName)
                        {
                            connectionManager.RunCommand(device, command);
                        }
                    }
                }
            }

            CommandLineTextBox.Text = null;
        }

        private void CommandLineTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendCommandButton.PerformClick();
            }
        }

        private void RunScriptButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in DevicesDataGrid.Rows)
            {
                if (row.Selected)
                {
                    foreach (Device device in ConnectedDevices)
                    {
                        if (row.Cells[0].Value.ToString() == device.PortName)
                        {
                            Task.Run(() => RunScript(device));
                        }
                    }
                }
            }
        }

        private void RunScript(Device device)
        {
            int count = 0;
            int progressnum = 1;

            IProgress<int> progress = new Progress<int>(value =>
            {
                BeginInvoke(new Action(() => { ProgressBar.Value = value; }));
            });

            foreach (DataGridViewRow row in ScriptCommandsDataGrid.Rows)
            {
                count++;
                var percentComplete = (progressnum * 100) / ScriptCommandsDataGrid.Rows.Count;
                progressnum++;
                progress.Report(percentComplete);
                device.PortConnection.Write($"{row.Cells["Instruction"].Value}\r");
                Thread.Sleep(Convert.ToInt32(row.Cells["Delay"].Value));
            }
            BeginInvoke(new Action(() => { ProgressBar.Value = 0; }));
        }

        private void ClearConsoleButton_Click(object sender, EventArgs e)
        {
            ConsoleTextBox.Text = null;
        }

        private void AddScriptCommandButton_Click(object sender, EventArgs e)
        {
            foreach (var item in LoadedScripts)
            {
                if (item.Name == ScriptTextBox.Text)
                {
                    Command command = new("ENTER COMMAND", "ENTER DESCRIPTION", 1000);
                    item.Commands.Add(command);
                    ScriptCommandsDataGrid.DataSource = item.Commands;
                    ScriptCommandsDataGrid.Refresh();
                }
            }
        }

        private void DeleteScriptCommandButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in ScriptCommandsDataGrid.Rows)
            {
                if (row.Selected)
                {
                    foreach (var item in LoadedScripts)
                    {
                        if (item.Name == ScriptTextBox.Text)
                        {
                            Command commandtodelete = new(row.Cells["Instruction"].Value.ToString(), row.Cells["Description"].Value.ToString(), Convert.ToInt32(row.Cells["Delay"].Value));

                            List<Command> commandstodelete = new();

                            foreach (var command in item.Commands)
                            {
                                if (command.Instruction == commandtodelete.Instruction && command.Description == commandtodelete.Description && command.Delay == commandtodelete.Delay)
                                {
                                    commandstodelete.Add(command);
                                }
                            }
                            foreach (var delete in commandstodelete)
                            {
                                item.Commands.Remove(delete);
                            }
                        }
                    }
                }
            }
        }
    }
}