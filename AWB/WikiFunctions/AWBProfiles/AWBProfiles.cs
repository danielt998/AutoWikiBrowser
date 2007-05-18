using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WikiFunctions.AWBProfiles
{
    public partial class AWBProfilesForm : Form
    {
        private WikiFunctions.Browser.WebControl Browser;
        AWBProfile AWBProfile = new AWBProfile();

        public AWBProfilesForm(WikiFunctions.Browser.WebControl browser)
        {
            InitializeComponent();
            this.Browser = browser;
        }

        private void AWBProfiles_Load(object sender, EventArgs e)
        {
            loadProfiles();
        }

        private void loadProfiles()
        {
            foreach (Profile profile in AWBProfile.GetProfiles())
            {
                ListViewItem item = new ListViewItem(profile.username);
                if (profile.password != "")
                    item.SubItems.Add("Yes");
                else
                    item.SubItems.Add("No");
                item.SubItems.Add(profile.defaultsettings);
                item.SubItems.Add(profile.notes);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (lvAccounts.SelectedItems.Count == 1)
            {
                if (lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text == "Yes")
                {//Get 'Saved' Password
                    //Browser.Login(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text, "");
                }
                else
                {//Get Password from User
                    //Browser.Login(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text, "");
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AWBProfileAdd add = new AWBProfileAdd();
            if (add.ShowDialog() == DialogResult.Yes)
            {
                loadProfiles();
            }
        }
    }
}