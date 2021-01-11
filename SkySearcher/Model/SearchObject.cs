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
        int i = 0;

        public delegate void ProgressBarDelegate(int count);
        public event ProgressBarDelegate ProgressBarCountEvent;

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

            if (!string.IsNullOrEmpty(msg))
                Task.Run(() => MessageBox.Show($"{msg} не найдены"));

            entry.Close();

            return temp;
        }

        /// <summary>
        /// Медо записи даных в AD
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="entries"></param>
        public async Task InputSomePcInv(ObservableCollection<AttributeValueObject> attributes, List<DirectoryEntry> entries)
        {
            foreach (var t in attributes)
            {
                DirectoryEntry temp = new DirectoryEntry();

                // Выбор каждого отдельного ПК
                await Task.Run(() => temp = entries.Where(x => x.Name.Contains(t.AttributePcValue)).FirstOrDefault());

                string tempSelectedDepInv = t.SelectDepInv;

                if (temp != null)
                {
                    tempSelectedDepInv = CheckSelectedDep(tempSelectedDepInv);

                    try
                    {
                        if (string.IsNullOrEmpty(tempSelectedDepInv))
                            throw new NullException("Не выбрана система учета");
                        else if (string.IsNullOrEmpty(t.InputInv))
                            throw new NullException("Не заполнен инвентерный номер");
                        else
                            await InputInv(temp, tempSelectedDepInv + t.InputInv);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.Message);
                    }
                }
            }
        }

        private string CheckSelectedDep(string tempSelectedDepInv)
        {
            switch (tempSelectedDepInv.ToLower())
            {
                case "дл транс (600)":
                    tempSelectedDepInv = "ДТ";
                    break;
                case "колл центр (296cc)":
                    tempSelectedDepInv = "КЦ";
                    break;
                case "колл центр (296)":
                    tempSelectedDepInv = "296";
                    break;
                case "грузоперевозки (296)":
                    tempSelectedDepInv = "296";
                    break;
                default:
                    tempSelectedDepInv = null;
                    break;
            }

            return tempSelectedDepInv;
        }

        /// <summary>
        /// Метод записи инвентарного номера в AD
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="input"></param>
        public async Task InputInv(DirectoryEntry entry, string input)
        {
            try
            {
                // Присваивание значения определенному атрибуту в AD
                entry.Properties["extensionAttribute7"].Value = $"{input}";
                // Сохранение данных в AD
                await Task.Run(() => entry.CommitChanges());

                ProgressBarCountEvent?.Invoke(i += 1);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
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

        //public string ValidatePcName(string pcNames)
        //{
        //    Regex regex = new Regex(@"^[a-zA-Z]{3,10}-[0-9]{3,4}");

        //    string msg = string.Empty;

        //    if(!regex.IsMatch(pcNames))
        //    {
        //        msg = msg + $"{pcNames} ";
        //    }

        //    return msg;
        //}
    }
}
