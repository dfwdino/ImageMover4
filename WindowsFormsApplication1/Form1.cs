using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SDN.Programs
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private ImageList il;
        private ListViewItem lvi;
        private string LookInFolder;
        private App_Code.ProgramInfo myPI = new App_Code.ProgramInfo();
        ListView.CheckedListViewItemCollection cic = null;

        private List<string> ListOfFiles = new List<string>();
        int LoadedImages = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Image Mover - " + GetAsseblyVersion();
            
            LookInFolder = myPI.LookInFolder;

            if (LookInFolder.Length.Equals(0))
            {
                myPI.LookInFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                LookInFolder = myPI.LookInFolder;
            }

            if (Directory.Exists(LookInFolder))
                ListOfFiles = new List<string>(System.IO.Directory.GetFiles(LookInFolder)
                    .Where(m => (m.ToLower().EndsWith("jpg") || m.ToLower().EndsWith("png") 
                                    || m.ToLower().EndsWith("bmp") || m.ToLower().EndsWith("jpeg"))
                        && m.Length > 2)
                    .Select(f => new FileInfo(f).FullName)
                    .OrderByDescending(f => f.Length));//need to make this a function
            else
            {
                myPI.LookInFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                
                LookInFolder = myPI.LookInFolder;
                ListOfFiles = new List<string>(System.IO.Directory.GetFiles(LookInFolder).Where(m =>
                m.ToLower().EndsWith("jpg") || m.ToLower().EndsWith("png"))
                    .OrderBy(f => new FileInfo(f).Length));
            }

            SetImages(LookInFolder);

            foreach (var item in myPI.MoveToFolders.AsEnumerable().OrderBy(r => ((string) r["LookAtLocation"])).ToList())
            {
                lsbMoveLocation.Items.Add(item[0]);
            }

            myPI.SaveData();

            lblStatus.Text = string.Format("Looking in {0}. Number of files {1}", LookInFolder, ListOfFiles.Count);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            if (SelectFiles(true))
                SetImages(LookInFolder);
        }

        private string GetAsseblyVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            return asm.GetName().Version.ToString();
        }

        private bool SelectFiles(bool Move)
        {
            if (lsvImages.CheckedItems.Count.Equals(0))
            {
                MessageBox.Show("No Items Selected.");
                return false;
            }
            else if (lsbMoveLocation.SelectedItems.Count.Equals(0) && Move)
            {
                MessageBox.Show("Location to move is not selected.");
                return false;
            }

            cic = lsvImages.CheckedItems;

           

            for (int i = cic.Count - 1; i >= 0; i--)
            {
                string ta = (string)cic[i].Tag;
                int ImageIndex = cic[i].Index;

                FileInfo fi = new FileInfo(ta);

                if (Move)
                {
                    
                    try
                    {
                        string filename = fi.Name;

                        if (System.IO.File.Exists(lsbMoveLocation.SelectedItem + "\\" + filename))
                        {
                            Random rd = new Random(23423);
                            filename = filename.Replace(".", rd.Next(0, 234232).ToString() + ".");
                        }

                        string tempfilename = lsbMoveLocation.SelectedItem + "\\" + filename;

                        if (System.IO.File.Exists(tempfilename))
                        {   
                            Random rd = new Random();

                            tempfilename = lsbMoveLocation.SelectedItem + "\\" + rd.Next(2354321).ToString() + "_" + filename;
                        }
                        System.IO.File.Move(ta, tempfilename);
                        ListOfFiles.RemoveAt(ImageIndex);
                        lsvImages.Items.RemoveAt(ImageIndex);
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    
                    try
                    {
                        ListViewItem temp = cic[i];

                        System.IO.File.Delete(ta);
                        ListOfFiles.RemoveAt(temp.ImageIndex);

                        lsvImages.Items.Remove(temp);
                        
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                    }
                }
            }
            cic = null;
            lsvImages.Items.Clear();
            

            return true;
        }

        

        private void SetImages(string LookInFolder)
        {
            
            lblStatus.Text = string.Format("Looking in {0}. Number of files {1}", LookInFolder, ListOfFiles.Count);

           
            il = new ImageList();
            il.ImageSize = new Size(72, 72);

            il.ColorDepth = ColorDepth.Depth16Bit;
          
            lsvImages.CheckBoxes = true;

            lvi = new ListViewItem();
            //ListOfFiles.Sort();

            for (int i = lsvImages.Items.Count; i < Convert.ToInt16(txtNumberofImagesToShow.Text) && ListOfFiles.Count > i ; i++)
            {
                lvi = new ListViewItem();
                
                string file = ListOfFiles[i];


                if (!File.Exists(file))
                {
                    ListOfFiles.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                FileInfo fi = new FileInfo(file);
                
                try
                {
                    Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
                    Bitmap myBitmap = new Bitmap(file);
                    Image myThumbnail = myBitmap.GetThumbnailImage(75, 75, myCallback, IntPtr.Zero);
                    //pvm.RealImages.Add(myThumbnail);

                    //ImageConverter _imageConverter = new ImageConverter();
                    //byte[] xByte = (byte[])_imageConverter.ConvertTo(myThumbnail, typeof(byte[]));

                    il.Images.Add(myThumbnail);
                    myThumbnail = null;
                    myBitmap = null;
                    
                    lsvImages.LargeImageList = il;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    continue;
                    
                }
                
                

                lvi = new ListViewItem();
                lvi.Text = fi.Name;
                lvi.Tag = file;
                lvi.ImageIndex = i;

                lsvImages.Items.Add(lvi);

                lvi = null;

                
            }

            il = null;

            GC.Collect();

            
        }

        private void btnChangeLookLocation_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            ofd.ShowNewFolderButton = true;
            ofd.Description = "Select Folder.";
            ofd.ShowNewFolderButton = false;
            //PostionListOfFiles = 0;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LookInFolder = ofd.SelectedPath;
                myPI.LookInFolder = LookInFolder;
                myPI.SaveData();
            }

            ListOfFiles = new List<string>(System.IO.Directory.GetFiles(LookInFolder).OrderBy(f => new FileInfo(f).Length));

            lsvImages.Clear();
            lsvImages.Refresh();
            

            SetImages(LookInFolder);
        }

        private void btnAddMoveLocation_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            ofd.ShowNewFolderButton = true;
            ofd.Description = "Select Folder.";
            
            ofd.ShowNewFolderButton = false;
            ofd.ShowNewFolderButton = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lsbMoveLocation.Items.Add(ofd.SelectedPath);
                myPI.AddMoveToFolders(ofd.SelectedPath);

                myPI.SaveData();
            }
        }

        private void btnMoveLocation_Click(object sender, EventArgs e)
        {
            if (lsbMoveLocation.SelectedItem == null)
            {
                MessageBox.Show("Nothing selected.");
                return;
            }
            myPI.DeleteMoveToFolders(lsbMoveLocation.SelectedItem.ToString());

            lsbMoveLocation.Items.RemoveAt(lsbMoveLocation.SelectedIndex);

            myPI.SaveData();
        }

        private void btnDeleteSelectedImages_Click(object sender, EventArgs e)
        {
            SelectFiles(false);
            lsvImages.Clear();
            SetImages(LookInFolder);
            
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (lsvImages.SelectedItems.Count.Equals(1))
            {
                picB.SizeMode = PictureBoxSizeMode.StretchImage;

                try
                {
                    FileStream fs = new FileStream(lsvImages.SelectedItems[0].Tag.ToString(), System.IO.FileMode.Open);
                    Image img = Image.FromStream(fs);
                    fs.Close();
                    picB.Image = img;

                    FileInfo fi = new FileInfo(lsvImages.SelectedItems[0].Tag.ToString());

                    lblFileInfo.Text = string.Format("File Info: {0} - {1}KB", fi.Name, fi.Length/1024);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        
        private void btnResetNumber_Click(object sender, EventArgs e)
        {
            LoadedImages = 0;
            lsvImages.Clear();
            SetImages(LookInFolder);
        }

        public bool ThumbnailCallback()
        {
            return false;
        }

        private void lsbMoveLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblMoveLocationSelected.Text = lsbMoveLocation.SelectedItem == null ? string.Empty : lsbMoveLocation.SelectedItem.ToString();
        }
    }
}