using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace CreateUser
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void createNewUser(object sender, EventArgs e)
        {

 
                string vorname = vornameBox.Text;
                string name = nameBox.Text;

                string location = radioButton1.Checked ? "SR1" : "LA1";

                ActiveDirectoryConnector connector = new ActiveDirectoryConnector();

                string username = "";
                if (usernameBox.Text.Length > 0)
                    username = usernameBox.Text;
                else
                    username = connector.createUserName(vorname, name);
            try
                {
  
                }
                catch (Exception ex)
                {
                    logerror("Username erzeugen fehlgeschlagen: " + ex.Message);
                    return;
                }
                log("Username ist " + username);

                //create the useraccount
                if (!connector.createUserAccount(vorname, name, username))
                {
                    logerror("Useraccount konnte nicht angelegt werden.");
                    return;
                }
                log("Useraccount angelegt.");

                //set password and password properties
                string password = "";
                password = connector.generateRandomPassword();
                if (!connector.setPassword(username, password))
                {
                    logerror("Passwort setzen fehlgeschlagen.");
                    return;
                }
                log("Passwort: " + password);
            
                //put password into DB
                if (!connector.writePasswordToMySQL(username, password))
                {
                    logerror("Konnte Passwort nicht in DB speichern.");
                    return;
                }
                log("Passwort in DB gespeichert");

                //create a group and link user and group
                string groupName = "G" + username.ToUpperInvariant();
                if (!connector.createGroup(groupName))
                {
                    logerror("Anlegen der Gruppe " + groupName + " fehlgeschlagen");
                    return;
                }
                log("Gruppe " + groupName + "angelegt.");
                if (!connector.addUserToGroup(username, groupName))
                {
                    logerror("Konnte User nicht zu Gruppe hinzufügen.");
                    return;
                }
                log("User zur Gruppe hinzugefügt");


                //set unix attributes
                Int32 nextID = connector.getNextUID();
                if (nextID == 0)
                {
                    logerror("Fehler beim Finden der nächsten uid");
                    return;
                }
                log("Nächste uid gefunden");

                //gruppenattribute setzen

                if (connector.unixAttributesGroupAlreadySet(groupName))
                {
                    log("Gruppenattribute bereits vorhanden");
                }
                else
                {
                    if (connector.setUnixAttributesGroup(groupName, nextID))
                        log("Gruppenattribute gesetzt");
                    else
                    {
                        logerror("Fehler beim Setzen der Gruppenattribute");
                        return;
                    }
                }

                //userattribute setzen            
                if (connector.unixAttributesUserAlreadySet(username))
                {
                    log("Userattribute bereits vorhanden");
                }
                else
                {
                    if (connector.setUnixAttributesUser(username, groupName, nextID, location,vorname, name))
                        log("Userattribute gesetzt");
                    else
                    {
                        logerror("Fehler beim Setzen der Userattribute");
                        return;
                    }
                }
                log("--------------------------------------------------------");

        }

        private void showNextGUID(object sender, EventArgs e)
        {
            ActiveDirectoryConnector connector = new ActiveDirectoryConnector();
            log("Nächste GID: " + connector.getNextUID());
            log("--------------------------------------------------------");
        }


        private void logerror(string p)
        {
            textBox1.AppendText("FEHLER: "+p);
            textBox1.AppendText("--------------------------------------------------------");
        }

        private void log(string p)
        {
            textBox1.AppendText(p+"\n");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }


        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter_1(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.createNewUser(sender, e);
        }

        private void label3_Click_1(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            
            ActiveDirectoryConnector connector = new ActiveDirectoryConnector();
            this.passwordBox.Text = connector.generateRandomPassword();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ActiveDirectoryConnector connector = new ActiveDirectoryConnector();
            try
            {
                if (connector.setPassword(this.changeUsername.Text, this.passwordBox.Text))
                    log("Passwort gesetzt");
                else
                {
                    logerror("Passwort setzen fehlgeschlagen");
                    return;
                }

                if (connector.writePasswordToMySQL(this.changeUsername.Text, this.passwordBox.Text))
                    log("Passwort in DB geschrieben");
                else
                {
                    logerror("Passwort in DB schreiben fehlgeschlagen");
                    return;
                }
                log("--------------------------------------------------------");
            }
            catch(Exception ex){
                logerror("Ein Fehler ist aufgetreten.");
            }
        }
    }
}
