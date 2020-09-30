using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SkySearcher.Model
{
    class SearchObject
    {
        /// <summary>
        /// Метод поиска ПК в AD
        /// </summary>
        /// <param name="pcName"></param>
        /// <returns></returns>
        public List<DirectoryEntry> SearchPc(List<string> pcName)
        {
            string msg = string.Empty;

            DirectoryEntry entry = new DirectoryEntry();

            // Коллекция свойств из AD по введенным ПК
            List<DirectoryEntry> temp = new List<DirectoryEntry>();

            // Цикл получения свойств по каждому отдельному ПК
            foreach (var t in pcName)
            {
                DirectorySearcher search = new DirectorySearcher(entry, $"(&(ObjectClass=Computer)(Name={t}))");

                // Результат поиска ПК
                SearchResult result = search.FindOne();

                try
                {
                    temp.Add(result.GetDirectoryEntry());
                }
                catch
                {
                    msg = msg + $"{t}; ";

                    continue;
                }
            }

            if (!string.IsNullOrEmpty(msg))
                Task.Run(() => MessageBox.Show($"{msg} не найдены"));

            entry.Close();

            return temp;
        }

        /// <summary>
        /// Метод поиска ПК в AD
        /// </summary>
        /// <param name="excels"></param>
        /// <returns></returns>
        public List<DirectoryEntry> SearchPc(List<ExcelData> excels)
        {
            string msg = string.Empty;

            DirectoryEntry entry = new DirectoryEntry();

            List<DirectoryEntry> temp = new List<DirectoryEntry>();

            foreach (var t in excels)
            {
                DirectorySearcher search = new DirectorySearcher(entry, $"(&(ObjectClass=Computer)(Name={t.PcName}))");

                SearchResult result = search.FindOne();

                try
                {
                    temp.Add(result.GetDirectoryEntry());
                }
                catch
                {
                    msg = msg + $"{t.PcName}; ";

                    continue;
                }
            }

            if(!string.IsNullOrEmpty(msg)) 
                Task.Run(() => MessageBox.Show($"{msg} не найдены"));

            entry.Close();

            return temp;
        }

        /// <summary>
        /// Медо получения даных из AD
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="entries"></param>
        public void InputSomePcInv(ObservableCollection<AttributeValueObject> attributes, List<DirectoryEntry> entries)
        {
            foreach (var t in attributes)
            {
                // Выбор каждого отдельного ПК
                var temp = entries.Where(t.AttributePcValue);

                if (temp != null)
                {
                   InputInv(temp, t.SelectDepInv + t.InputInv);
                }
                else
                {
                    MessageBox.Show("Пусто");
                }
            }
        }

        /// <summary>
        /// Метод записи инвентарного номера в AD
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="input"></param>
        public void InputInv(DirectoryEntry entry, string input)
        {
            try
            {
                // Присваивание значения определенному атрибуту в AD
                entry.Properties["extensionAttribute7"].Value = $"{input}";
                // Сохранение данных в AD
                entry.CommitChanges();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// Метод присваивания данных экзкмпляру класса AttributeValueObject для вывода в форму и изменения данных
        /// </summary>
        /// <param name="entrie"></param>
        /// <param name="attribute"></param>
        /// <param name="depNum"></param>
        /// <param name="invNum"></param>
        /// <returns></returns>
        public AttributeValueObject AddAttributeValue(DirectoryEntry entrie, AttributeValueObject attribute, string depNum, string invNum)
        {
            attribute = new AttributeValueObject();

            // Условие заполнения инвертарного номера для отображения
            if (entrie.Properties["extensionAttribute7"].Value == null)
            {
                attribute.AttributeDescValue = "Не заполнено";
            }
            else
            {
                attribute.AttributeDescValue = entrie.Properties["extensionAttribute7"].Value.ToString();
            }

            // Присваивание значения сойств для вывода в форму
            attribute.AttributePcName = entrie.Properties["cn"].PropertyName;
            attribute.AttributePcValue = entrie.Properties["cn"].Value.ToString();
            attribute.AttributeDescName = entrie.Properties["extensionAttribute7"].PropertyName;
            attribute.SelectDepInv = depNum;
            attribute.InputInv = invNum;
            
            return attribute;
        }
    }
}
