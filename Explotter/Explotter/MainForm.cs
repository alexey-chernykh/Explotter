using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explotter
{
    public partial class MainForm : Form
    {
        int count = 0;
        string currentPath = Path.GetPathRoot(Environment.SystemDirectory);
        List<string> previousPath = new List<string>();

        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int WM_DEVICECHANGE = 0x0219;
        public MainForm()
        {
            InitializeComponent();
        }
        [DllImport("Shell32.dll", EntryPoint = "ExtractAssociatedIcon")]
        extern static IntPtr ExtractAssociatedIconAPI(IntPtr hInst, String iconPath, ref int piIcon);
        [DllImport("Kernel32.dll")]
        extern static IntPtr GetModuleHandle(String moduleName);
        static Icon ExtractAssociatedIcon(String iconPath)
        {
            IntPtr moduleHandle = GetModuleHandle(null);
            int piIcon = 0;
            IntPtr hIcon = ExtractAssociatedIconAPI(moduleHandle, iconPath, ref piIcon);
            return Icon.FromHandle(hIcon);
        }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg == WM_DEVICECHANGE && m.WParam == (IntPtr)DBT_DEVICEARRIVAL || m.WParam == (IntPtr)DBT_DEVICEREMOVECOMPLETE)
            {
                FillDrives();
            }
            base.WndProc(ref m);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            FillDrives();
            FillFilePanel();

            String my = "My Computer";
            Image icon = Bitmap.FromFile("comp.ico");
            imageListTree.Images.Add("/",icon);
            imageListTree.Images.Add("/", icon);

            TreeNode rootNode = new TreeNode(my, 0, 1);
            rootNode.ImageKey = "/";
            rootNode.Nodes.Add(new TreeNode("empty"));
            treeView1.Nodes.Add(rootNode);
        }

        private void FillFilePanel()
        {
            listViewFiles.Items.Clear();
            imageListLarge.Images.Clear();
            imageListSmall.Images.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(currentPath);
            foreach(DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                var item = listViewFiles.Items.Add(dir.Name);
                item.SubItems.Add("DIR");
                item.SubItems.Add(dir.CreationTime.ToString("dd.MM.yyyy"));
                item.SubItems.Add(dir.Attributes.ToString());
                imageListSmall.Images.Add(dir.FullName, ExtractAssociatedIcon(dir.FullName));
                imageListLarge.Images.Add(dir.FullName, ExtractAssociatedIcon(dir.FullName));
                item.ImageKey = dir.FullName;
            }
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                var item = listViewFiles.Items.Add(file.Name);
                item.SubItems.Add(file.Length.ToString());
                item.SubItems.Add(file.CreationTime.ToString("dd.MM.yyyy"));
                item.SubItems.Add(file.Attributes.ToString());
                imageListSmall.Images.Add(file.FullName, ExtractAssociatedIcon(file.FullName));
                imageListLarge.Images.Add(file.FullName, ExtractAssociatedIcon(file.FullName));
                item.ImageKey = file.FullName;
                
            }
        }

        private void FillDrives()
        {
            toolStripDrives.Items.Clear();
            foreach (var drv in Directory.GetLogicalDrives())
            {
                var item = toolStripDrives.Items.Add(drv.Substring(0,2));
                item.Image = ExtractAssociatedIcon(drv).ToBitmap();
                item.Click += (o, e) =>
                  {
                      count = 0;
                      previousPath.Add(currentPath);
                      currentPath = drv;
                      FillFilePanel();
                  };
            }
        }

        private void listViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems[0].SubItems[1].Text == "DIR")
            {
                ReloadListViewFiles(0);
            }
            else
            {
                String path = Path.Combine(currentPath, listViewFiles.SelectedItems[0].Text);
                Process.Start(path);
            }
            
        }

        private void ReloadListViewFiles(int operation)
        {
            previousPath.Add(currentPath);
            switch (operation)
            {
                case 0:
                    currentPath = Path.Combine(currentPath, listViewFiles.SelectedItems[0].Text);
                    break;
                case 1:
                    if (treeView1.SelectedNode.Text == "My Computer")
                    {
                        DoNothing();
                    }
                    else
                    {
                        currentPath = treeView1.SelectedNode.FullPath.Substring(12, treeView1.SelectedNode.FullPath.Length - 12);
                    }
                    break;
                default:
                    break;
            }
            
            
            FillFilePanel();
        }

        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                currentPath = previousPath[(previousPath.Count - 1) - count];
                count++;
                FillFilePanel();
            }
            catch
            {
                DoNothing();
            }
        }

        private void DoNothing()
        {
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;

            foreach (TreeNode n in node.Nodes)
            {
                imageListTree.Images.RemoveByKey(n.ImageKey);
                imageListTree.Images.RemoveByKey(n.SelectedImageKey);
            }

            node.Nodes.Clear();
            node.Nodes.Add("empty");
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {

        }

        private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {

        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node ==treeView1.Nodes[0])
            {
                node.Nodes.Clear();
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    TreeNode currentNode = node.Nodes.Add(drive.Substring(0,2));
                    currentNode.Nodes.Add("empty");
                    currentNode.Tag = drive;
                    currentNode.ImageKey = drive;
                    imageListTree.Images.Add(drive, ExtractAssociatedIcon(drive));

                }
            }
            else
            {
                node.Nodes.Clear();
                foreach (var path in Directory.GetDirectories(node.Tag.ToString()))
                {
                    TreeNode currentNode = node.Nodes.Add(Path.GetFileName(path));
                    currentNode.Nodes.Add("empty");
                    currentNode.Tag = path;
                    currentNode.ImageKey = path;
                    imageListTree.Images.Add(path, ExtractAssociatedIcon(path));

                }
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            
            ReloadListViewFiles(1);
        }
    }
}
