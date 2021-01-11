﻿using SkySearcher.HelpedClasses;
using SkySearcher.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkySearcher.ViewModel
{
    class MainViewModel : PropChanged
    {
        /// <summary>
        /// Поле экзмпляра класса диалога выбора файла
        /// </summary>
        private SaveOpen saveOpen;

        /// <summary>
        /// Поле экземпляра класса поиска и записи данных
        /// </summary>
        private SearchObject search;

        private string searchPcName;
        /// <summary>
        /// Свойство значения для поска ПК
        /// </summary>
        public string SearchPcName
        {
            get { return searchPcName; }
            set { searchPcName = value; OnPropertyChanged(nameof(SearchPcName)); }
        }

        private ObservableCollection<AttributeValueObject> getingProp;
        /// <summary>
        /// Свойство коллекция значений свойств полученных атрибутов
        /// </summary>
        public ObservableCollection<AttributeValueObject> GetingProp
        {
            get { return getingProp; }
            set { getingProp = value; OnPropertyChanged(nameof(GetingProp)); }
        }

        /// <summary>
        /// Коллекция найденных ПК
        /// </summary>
        private List<DirectoryEntry> entrys = new List<DirectoryEntry>();

        /// <summary>
        /// Поле экземпляра класса данных выгруженных из файла Excel
        /// </summary>
        private List<ExcelData> excelDatas;

        private string path;
        /// <summary>
        /// Свойство пути к файлу Excel
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; OnPropertyChanged(nameof(Path)); }
        }

        private bool activeMaster;
        /// <summary>
        /// Свойство активности мастер-режима
        /// </summary>
        public bool ActiveMaster
        {
            get { return activeMaster; }
            set { activeMaster = value; OnPropertyChanged(nameof(ActiveMaster)); }
        }

        private int maxProgBar = 1;
        /// <summary>
        /// Свойство максимального значения прогресс-бара
        /// </summary>
        public int MaxProgBar
        {
            get { return maxProgBar; }
            set { maxProgBar = value; OnPropertyChanged(nameof(MaxProgBar)); }
        }

        private int valueProgBar;
        /// <summary>
        /// Свойство текущего значения прогресс-бара
        /// </summary>
        public int ValueProgBar
        {
            get { return valueProgBar; }
            set { valueProgBar = value; OnPropertyChanged(nameof(ValueProgBar)); }
        }

        private bool indeterminate;
        /// <summary>
        /// Свойство включения бесконечной прокрутки прогесс-бара
        /// </summary>
        public bool Indeterminate
        {
            get { return indeterminate; }
            set { indeterminate = value; OnPropertyChanged(nameof(Indeterminate)); }
        }

        private AttributeValueObject selectedPC;
        /// <summary>
        /// Свойство выбранного ПК в списке найденных ПК
        /// </summary>
        public AttributeValueObject SelectedPC
        {
            get => selectedPC;
            set { selectedPC = value; OnPropertyChanged(nameof(SelectedPC)); }
        }

        /// <summary>
        /// Команда получения данных из AD
        /// </summary>
        public ICommand GetEntry => new CommandClass<object>(async obj =>
        {
            await GetEntries(obj);
        });

        /// <summary>
        /// Команда удаления инвентарного номера выбранного ПК
        /// </summary>
        public ICommand RemoveInv => new CommandClass<object>(async obj =>
        {
            // Проверка на пустое значение выбранного ПК
            if (SelectedPC != null)
            {
                #region _
                //SelectedPC.SelectDepInv = " "; //
                //                               // Установка пустых значений для удаления инв.номера
                //SelectedPC.InputInv = " ";     //
                #endregion
                // Выбор свойств ПК из AD
                var pcEntry = entrys.Where(x => x.Name.Contains(SelectedPC.AttributePcValue)).FirstOrDefault();

                // Условие подтверждение удаления инв.номера
                if (MessageBox.Show($@"Удалить инв.номер ""{SelectedPC.AttributeDescValue}"" у компьютера {SelectedPC.AttributePcValue}?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await search.InputInv(pcEntry, " ");

                    await GetEntries(obj);
                }
            }
            else
            {
                MessageBox.Show("Не выбрана запись ПК");
            }
        });

        /// <summary>
        /// Команда внесения инвентраного номера для одного или нескольких ПК
        /// </summary>
        public ICommand Input => new CommandClass<object>(async obj =>
        {
            SearchObject search = new SearchObject();

            search.ProgressBarCountEvent += Search_ProgressBarCountEvent;

            ValueProgBar = 0;

            MaxProgBar = GetingProp.Count;

            try
            {
                await search.InputSomePcInv(GetingProp, entrys);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            await GetEntries(obj);
        });

        private void Search_ProgressBarCountEvent(int count)
        {
            ValueProgBar = count;
        }

        /// <summary>
        /// Команда выбора файла
        /// </summary>
        public ICommand OpenFile => new CommandClass<object>(async obj =>
        {
            SearchPcName = string.Empty;

            // Получение пути к файлу Excel
            Path = saveOpen.OpenFile();

            // Проверка пустого значения пути
            if (!string.IsNullOrEmpty(Path))
            {
                ExcelWork excelWork = new ExcelWork(path);

                // Временная коллекция объектов ПК
                var tempList = new List<AttributeValueObject>();

                await Task.Run(() =>
                {
                    Indeterminate = true;

                    // Получение данных из файла Excel
                    excelDatas = excelWork.GetDataFromXl();

                    // Поиск ПК указанных в файле Excel
                    entrys = search.SearchPc(excelDatas);

                    Indeterminate = false;

                    MaxProgBar = entrys.Count();

                    for (int i = 0; i < entrys.Count; i++)
                    {
                        // Получение данных из AD
                        tempList.Add(search.AddAttributeValue(entrys[i], new AttributeValueObject(), excelDatas[i].Dep, excelDatas[i].InvNum));

                        ValueProgBar = tempList.Count();
                    }
                });

                GetingProp = new ObservableCollection<AttributeValueObject>(tempList);

                GetPcNamesFromExcel();
            }
        });

        /// <summary>
        /// Команда проверки зашифрованного пароля для доступа к мастер режиму
        /// </summary>
        public ICommand PassOk => new CommandClass<object>(obj =>
        {
            var passBox = obj as PasswordBox;

            // Инициализация экземпляра класса декодирования пароля
            EncryptClass encrypt = new EncryptClass();

            // Условие проверки введенного пароля 
            if (encrypt.StartEncript(passBox.Password))
            {
                ActiveMaster = true;
            }
            else
            {
                MessageBox.Show("Неправильный пароль");
            }

            // Очистка поля ввода пароля
            passBox.Password = string.Empty;
        });

        /// <summary>
        /// Команда открытия окна с примером файла Excel
        /// </summary>
        public ICommand OpenExample => new CommandClass<object>(obj =>
            {
                new ExampleExcel().ShowDialog();
            });

        /// <summary>
        /// Конструктор
        /// </summary>
        public MainViewModel()
        {
            saveOpen = new SaveOpen();
            search = new SearchObject();
        }

        /// <summary>
        /// Метод получения данных из AD
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task GetEntries(object obj)
        {
            SearchPcName = obj as string;

            Indeterminate = true;

            // Разделение введенной строки на имена ПК
            List<string> temp = SearchPcName.Split(';').ToList();

            // Временная коллекция свойств ПК
            var tempList = new List<AttributeValueObject>();

            await Task.Run(() =>
            {
                // Проверка мастер-режима
                if (!ActiveMaster && temp.Count > 1)
                {
                    temp = new List<string> { temp[0] };
                    MessageBox.Show("Мастер режим отключен.\n\nБудет показан результат только по первому введенному имени ПК");
                }

                entrys = search.SearchPc(temp);

                Indeterminate = false;

                MaxProgBar = entrys.Count();

                foreach (var e in entrys)
                {
                    // Получение свойств ПК из AD
                    tempList.Add(search.AddAttributeValue(e, new AttributeValueObject(), string.Empty, string.Empty));

                    ValueProgBar = tempList.Count();
                }
            });

            GetingProp = new ObservableCollection<AttributeValueObject>(tempList);
        }

        private void GetPcNamesFromExcel()
        {
            foreach (var n in GetingProp)
            {
                if (n != GetingProp.Last())
                {
                    SearchPcName = SearchPcName + n.AttributePcValue + ';';
                }
                else
                {
                    SearchPcName = SearchPcName + n.AttributePcValue;
                }
            }
        }
    }
}
