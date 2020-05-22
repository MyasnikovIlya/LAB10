using System;
using System.Windows.Forms;
using SomeProject.Library.Client;
using SomeProject.Library;

namespace SomeProject.TcpClient
{
    public partial class ClientMainWindow : Form
    {
        public ClientMainWindow()
        {
            InitializeComponent();
        }
        private void disable()
        {
            textBox.ReadOnly = true;
            sendMsgBtn.Enabled = false;
            sendFileBtn.Enabled = false;
        }

        private void enable()
        {
            textBox.ReadOnly = false;
            sendMsgBtn.Enabled = true;
            sendFileBtn.Enabled = true;
        }
        private string sendMsg()
        {
            if (textBox.Text == "") return "Cant send empty message";
            Client client = new Client();
            OperationResult res = client.SendMessageToServer(textBox.Text);
            if (res.Result == Result.OK)
            {
                return "Message was sent succefully!";
            }
            else
            {
                return "Cannot send the message to the server." ;
            }

        }
        private void OnMsgBtnClick(object sender, EventArgs e)
        {
            disable();
            backgroundWorker1.RunWorkerAsync();
        }
        private string sendFile(string fileName)
        {
            Client client = new Client();
            var fileSplit = fileName.Split('.');
            var extention = fileSplit[fileSplit.Length - 1];
            OperationResult res = client.SendFileToServer(fileName, extention);
            if (res.Result == Result.OK)
            {
                return "file was sent succefully!";
            }
            else
            {
                return "Cannot send the file to the server.";
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            labelRes.Text = "";
            timer.Stop();
        }
        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var dialog = (FileDialog)sender;
            if (dialog.FileName != "")
            {
                disable();
                backgroundWorker2.RunWorkerAsync(dialog.FileName);
            }
        }

        private void sendFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            e.Result = sendMsg();
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            e.Result = sendFile((string)e.Argument);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            enable();
            labelRes.Text = (string)e.Result;
            timer.Interval = 5000;
            timer.Start();
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            enable();
            labelRes.Text = (string)e.Result;
            timer.Interval = 5000;
            timer.Start();
        }

        private void openFileDialog1_FileOk_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var dialog = (FileDialog)sender;
            if (dialog.FileName != "")
            {
                disable();
                backgroundWorker2.RunWorkerAsync(dialog.FileName);
            }
        }

        private void ClientMainWindow_Load(object sender, EventArgs e)
        {

        }
    }
}
