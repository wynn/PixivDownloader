using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;

namespace PixivUtilCS
{
    public partial class Form1 : Form
    {
        Pixiv pixiv;
        bool adultContent;
        Pixiv.ImageSearchOptions imageSearchOption;
        int pagesToDownload = 0;

        public Form1()
        {     
            InitializeComponent();
            comboBoxAdultContent.SelectedIndex = 0;
            
            comboBoxImageTypes.Items.Add(Pixiv.ImageSearchOptions.ALL);
            comboBoxImageTypes.Items.Add(Pixiv.ImageSearchOptions.ILLUSTRATIONS);
            comboBoxImageTypes.Items.Add(Pixiv.ImageSearchOptions.MANGA);
            comboBoxImageTypes.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                pixiv = new Pixiv(textBoxUsername.Text, textBoxPassword.Text);
            }
            catch
            {
                MessageBox.Show("Incorrect password or username!");
                return;
            }

            try
            {
                pagesToDownload = Convert.ToInt32(textBoxPages.Text);
            }
            catch
            {
                MessageBox.Show("Error parsing the amount of pages to download!");
                return;
            }

            StartThread(pixiv);
        }

        private void StartThread(Pixiv pixiv)
        {
            this.StatusLabel.Text = "Status: ";

            adultContent = comboBoxAdultContent.SelectedIndex == 1;
            imageSearchOption = (Pixiv.ImageSearchOptions)comboBoxImageTypes.SelectedItem;
            backgroundWorker1.RunWorkerAsync(pixiv);
            button1.Enabled = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Pixiv pixiv = (Pixiv)e.Argument;

            System.ComponentModel.BackgroundWorker worker;
            worker = (System.ComponentModel.BackgroundWorker)sender;

            pixiv.DownloadImages(worker, e, textBoxSearchTags.Text, 
                adultContent, imageSearchOption, 1, pagesToDownload);
        }
        
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PixivUtilCS.CurrentState state = (PixivUtilCS.CurrentState)e.UserState;
            StatusLabel.Text = "Status: " + state.Status;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show("Error: " + e.Error.Message);
            else if (e.Cancelled)
                MessageBox.Show("Canceled.");
            else
                MessageBox.Show("Finished downloading.");
            button1.Enabled = true;
            StatusLabel.Text = "Status: " + "Finished downloading";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
    }
}
