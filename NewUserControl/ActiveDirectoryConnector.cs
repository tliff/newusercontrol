using System;
using System.Xml.Linq;
using System.DirectoryServices;

namespace CreateUser
{
    class ActiveDirectoryConnector
    {

        private DirectoryEntry entry = null;


        private void checkConnection(){
            string connectionPrefix = "LDAP://verlag.vn.idowa.de";
            if(entry == null){
                entry = new DirectoryEntry(connectionPrefix);
            }
        }

        public Int32 getNextUID()
        {
            checkConnection();

            Int32 id = 0;

            DirectorySearcher mySearcher = new DirectorySearcher(entry);

            mySearcher.Filter = "(|(objectClass=user))";
            SearchResultCollection resultSet = mySearcher.FindAll();

            foreach (SearchResult result in resultSet)
            {
                DirectoryEntry user = result.GetDirectoryEntry();
                if (user.Properties["uidnumber"].Value != null)
                {
                    String foo = user.Properties["uidnumber"].Value.ToString();
                    String username = user.Properties["uid"].Value.ToString();
                    int thisID;
                    Int32.TryParse(foo, out thisID);
                    if (thisID > id){
                        id = thisID;
                    }
                    
                }

            }

            mySearcher.Dispose();

            return (id != 0) ? (id + 1) : 0;
        }

        public Int32 getNextGID()
        {
            checkConnection();

            Int32 id = 0;

            DirectorySearcher mySearcher = new DirectorySearcher(entry);

            mySearcher.Filter = "(|(objectClass=group)(objectClass=group))";
            SearchResultCollection resultSet = mySearcher.FindAll();

            foreach (SearchResult result in resultSet)
            {
                DirectoryEntry user = result.GetDirectoryEntry();
                if (user.Properties["gidNumber"].Value != null)
                {
                    String foo = user.Properties["gidNumber"].Value.ToString();
                    int thisID;
                    Int32.TryParse(foo, out thisID);
                    if (thisID > id)
                        id = thisID;
                }

            }

            mySearcher.Dispose();

            return (id != 0) ? (id + 1) : 0;
        }

        internal bool checkUserExists(string username)
        {
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=user)(sAMAccountName=" + username + "))";
            SearchResultCollection resultSet = mySearcher.FindAll();
            mySearcher.Dispose();
            return resultSet.Count > 0;
        }

        internal bool checkGroupExists(string groupName)
        {
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=group)(name=" + groupName + "))";
            SearchResultCollection resultSet = mySearcher.FindAll();
            mySearcher.Dispose();
            return resultSet.Count > 0;
        }

        internal string generateRandomPassword()
        {
            string lowerCase = "abcdefghjkmnopqrstuvwxyz";
            string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            string numbers = "23456789";

            Random r = new Random();

            string password;

            do {
                password = "";
                for (int i = 0; i < 8; i++) {
                    int type = r.Next(0, 5);
                    switch (type)
                    {
                        case 0:
                        case 3:
                            password += lowerCase.Substring(r.Next(0, lowerCase.Length), 1);
                            break;
                        case 1:
                        case 4:
                            password += upperCase.Substring(r.Next(0, upperCase.Length), 1);
                            break;
                        case 2:
                            password += numbers.Substring(r.Next(0, numbers.Length), 1);
                            break;
                    }
                }
            } while (!checkPassword(password));


            return password;
        }

        internal bool setUnixAttributesUser(string userName, string groupName, int uid, string location, string firstname, string lastname)
        {

            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=user)(sAMAccountName=" + userName + "))";
            SearchResult result = mySearcher.FindOne();
            mySearcher.Dispose();
            DirectoryEntry e = result.GetDirectoryEntry();

            DirectorySearcher mygSearcher = new DirectorySearcher(entry);
            mygSearcher.Filter = "(&(objectClass=group)(name=" + groupName + "))";
            SearchResult gresult = mygSearcher.FindOne();
            mygSearcher.Dispose();
            DirectoryEntry g = gresult.GetDirectoryEntry();


            e.Properties["msSFU30NisDomain"].Value = "verlag";

            int foo = int.Parse(g.Properties["gidNumber"].Value.ToString());

            e.Properties["gidNumber"].Value = int.Parse(g.Properties["gidNumber"].Value.ToString());

            e.Properties["unixHomeDirectory"].Value = "/home/"+location+"/"+userName;
            e.Properties["loginShell"].Value = "/bin/false";
            e.Properties["uid"].Value = userName;
            e.Properties["uidNumber"].Value = uid;
            e.Properties["gecos"].Value = firstname + " " + lastname;
            e.Properties["givenName"].Value = firstname;
            e.Properties["sn"].Value = lastname;

            e.Properties["description"].Value = firstname + " " + lastname;
            e.Properties["displayName"].Value = firstname + " " + lastname;

            e.Properties["homeDrive"].Value = "Z:";
            e.Properties["homeDirectory"].Value = "\\\\sr-home-1.vn.idowa.de\\"+userName;
            e.Properties["profilePath"].Value = "\\\\sr-home-1.vn.idowa.de\\profiles\\" + userName;
            e.CommitChanges();
            e.Close();

            return true;

        }

        internal bool setUnixAttributesGroup(string groupName, int nextID)
        {
            //\\archiv\edv-archiv\Sonstiges\ActiveDirectory\UserInstall\
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=group)(name=" + groupName + "))";
            SearchResult result = mySearcher.FindOne();
            mySearcher.Dispose();
            DirectoryEntry e = result.GetDirectoryEntry();

            e.Properties["msSFU30NisDomain"].Value = "verlag";
            //e.Properties["gidNumber"].Value = nextID;
            e.CommitChanges();
            e.Close();
            return true;
        }

        internal bool writePasswordToMySQL(string userName, string password)
        {
            string connectionString = "Database=useradm;Data Source=db0.vn.idowa.de;User id=mailadm;Password=fu8uhme8";
            MySql.Data.MySqlClient.MySqlConnection myConnection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            string query = "REPLACE INTO ad (username, passwort) VALUES('" + userName + "','" + password + "')";
            MySql.Data.MySqlClient.MySqlCommand command = new MySql.Data.MySqlClient.MySqlCommand(query);
            command.Connection = myConnection;
            myConnection.Open();
            command.ExecuteNonQuery();
            command.Connection.Close();
            return true;
        }

        internal bool setPassword(string userName, string password)
        {
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=user)(|(uid=" + userName + ")(cn=" + userName + ")))";
            SearchResult result = mySearcher.FindOne();
  
            DirectoryEntry e;
            e = result.GetDirectoryEntry();

            //password does not expire
            e.Properties["userAccountControl"].Value = 0x10200; 

            e.Invoke("SetPassword", new object[] { password });

            e.CommitChanges();
            e.Close();
            mySearcher.Dispose();
            //TODO: attribute setzen

            return true;
        }

        internal bool checkPassword(string password)
        {
            string lowerCase = "abcdefghijklmopqrstuvwxyz";
            string upperCase = "ABCDEFGHIJKLMOPQRSTUVWXYZ";
            string numbers = "1234567890";

            bool hasLower = false;
            bool hasUpper = false;
            bool hasNumber = false;

            CharEnumerator e  = password.GetEnumerator();
            while (e.MoveNext())
            {
                if (lowerCase.Contains(e.Current.ToString()))
                    hasLower = true;
                if (upperCase.Contains(e.Current.ToString()))
                    hasUpper = true;
                if (numbers.Contains(e.Current.ToString()))
                    hasNumber = true;
            }
            return (hasNumber && hasUpper && hasLower);
        }


        internal bool unixAttributesUserAlreadySet(string userName)
        {
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=user)(cn=" + userName + "))";
            SearchResult result = mySearcher.FindOne();
            mySearcher.Dispose();
            DirectoryEntry e = result.GetDirectoryEntry();

            return !(e.Properties["msSFU30NisDomain"].Value == null || e.Properties["msSFU30NisDomain"].Value.ToString().Length == 0);
        }

        internal bool unixAttributesGroupAlreadySet(string groupName)
        {
            checkConnection();
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=group)(name=" + groupName + "))";
            SearchResult result = mySearcher.FindOne();
            mySearcher.Dispose();
            DirectoryEntry e = result.GetDirectoryEntry();

            return !(e.Properties["msSFU30NisDomain"].Value == null || e.Properties["msSFU30NisDomain"].Value.ToString().Length == 0);
        }

        internal string createUserName(string vorname, string name)
        {
            vorname = vorname.ToLowerInvariant();
            name = name.ToLowerInvariant();
            string username = "";
            bool failage = true;
            int i = 0;
            do{
                username = 
                    (i != 8 ? name.Substring(0, Math.Min(8 - i, name.Length)) : "") + 
                    (i != 0 ? vorname.Substring(0, Math.Min(i,vorname.Length)) :"");
                i++;
            } while((failage = this.checkUserExists(username)) && i <= 8);
            if(failage)
                throw new NotImplementedException();
            return username;

        }

        internal bool createUserAccount(string vorname, string name, string username)
        {

            string oGUID = string.Empty;
            string connectionPrefix = "LDAP://verlag.vn.idowa.de/" + "OU=NewUsers,DC=verlag,DC=vn,DC=idowa,DC=de";
            DirectoryEntry dirEntry = new DirectoryEntry(connectionPrefix);
            DirectoryEntry newUser = dirEntry.Children.Add
                ("CN=" + username, "user");
            newUser.Properties["samAccountName"].Value = username;
            newUser.Properties["userAccountControl"].Value = 32;

            newUser.CommitChanges();

            oGUID = newUser.Guid.ToString();

            newUser.CommitChanges();
            dirEntry.Close();
            newUser.Close();

            return true;
        }

        internal bool addUserToGroup(string username, string groupName)
        {
            DirectoryEntry dirEntry = new DirectoryEntry("LDAP://verlag.vn.idowa.de/CN=" + groupName + ",OU=Gruppen,DC=verlag,DC=vn,DC=idowa,DC=de");
                dirEntry.Invoke("Add", new object[]{"LDAP://CN=" + username + ",OU=NewUsers,DC=verlag,DC=vn,DC=idowa,DC=de"});
                dirEntry.CommitChanges();
                dirEntry.Close();
           
            return true;
        }

        internal bool createGroup(string groupName)
        {
            string oGUID = string.Empty;
            string connectionPrefix = "LDAP://verlag.vn.idowa.de/" + "OU=Gruppen,DC=verlag,DC=vn,DC=idowa,DC=de";
            DirectoryEntry dirEntry = new DirectoryEntry(connectionPrefix);
            DirectoryEntry newGroup = dirEntry.Children.Add
                ("CN=" + groupName, "group");
            newGroup.Properties["samAccountName"].Value = groupName;
            newGroup.Properties["gidNumber"].Value = getNextGID();
            newGroup.CommitChanges();

            newGroup.CommitChanges();
            dirEntry.Close();
            newGroup.Close();
            return true;
        }
    }
}
