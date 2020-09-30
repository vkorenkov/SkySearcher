using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkySearcher.Model
{
    class EncryptClass
    {
        public bool StartEncript(string enteredPass)
        {
            string pass = string.Empty;

            if (File.Exists("Resources/pass"))
            {
                pass = EncryptWord(File.ReadAllText("Resources/pass"));
            }
            else
            {
                MessageBox.Show("Файл пароля не найден");
            }

            if(enteredPass == pass)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string EncryptWord(string word)
        {
            ushort key = 0x0128;

            var ch = word.ToArray();

            string newStr = "";
            foreach (var c in ch)
            {
                newStr += Encrypt(c, key);
            }

            return newStr;
        }

        private char Encrypt(char ch, ushort sKey)
        {
            ch = (char)(ch ^ sKey);
            return ch;
        }
    }
}
