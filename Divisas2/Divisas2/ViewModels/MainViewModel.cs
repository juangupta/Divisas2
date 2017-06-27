

using Divisas2.Models;
using Divisas2.Services;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using Plugin.Connectivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Windows.Input;

namespace Divisas2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion 

        #region Attributes
        //Cuando una propiedad tenga lógica crearsela basada en el atributo
        public bool isRunning;
        public bool isEnabled;
        private ExchangeRates exchangeRates;
        private NameRates nameRates;
        private string message;
        private double sourceRate;
        private double targetRate;
        private DataService dataService;
        private Preference preference;

        #endregion

        #region Properties
        public ObservableCollection<Rate> Rates { get; set; }
        public decimal Amount { get; set; }
        public double SourceRate
        {
            set
            {
                if (sourceRate != value)
                {
                    sourceRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceRate"));
                }
            }
            get
            {
                return sourceRate;
            }
        }
        public double TargetRate
        {
            set
            {
                if (targetRate != value)
                {
                    targetRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetRate"));
                }
            }
            get
            {
                return targetRate;
            }
        }

        public string Message
        {
            set
            {
                if (message != value)
                {
                    message = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message"));
                }
            }
            get
            {
                return message;
            }
        }
        public bool IsRunning
        {
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRunning"));
                }
            }
            get
            {
                return isRunning;
            }
        }

        public bool IsEnabled
        {
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));
                }
            }
            get
            {
                return isEnabled;
            }
        }


        #endregion

        #region Constructors
        public MainViewModel()
        {
            Rates = new ObservableCollection<Rate>();
            dataService = new DataService();
            IsEnabled = false;
            GetRates();
            preference = new Preference();
        }

        #endregion

        #region exchangeRates = dataService.First<ExchangeRates>(false);
                
        private async void GetRates()
        {
            IsRunning = true;
            IsEnabled = false;
            if (!CrossConnectivity.Current.IsConnected)
            {
                LoadRatesOffline();
                loadPreferences();
                Message = "La aplicación está trabajando Offline";
                IsRunning = false;
                IsEnabled = true;
                return;

            }
            var isRechable = await CrossConnectivity.Current.IsRemoteReachable("google.com");
            if (!isRechable)
            {
                LoadRatesOffline();
                loadPreferences();
                Message = "La aplicación está trabajando Offline";
                IsRunning = false;
                IsEnabled = true;
                return;
            }

            try
            {

                var client = new HttpClient();
                client.BaseAddress = new Uri("https://openexchangerates.org");
                var url = "/api/latest.json?app_id=f490efbcd52d48ee98fd62cf33c47b9e";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "Aceptar");
                    IsRunning = false;
                    IsEnabled = false;
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                exchangeRates = JsonConvert.DeserializeObject<ExchangeRates>(result);
                //dataService.DeleteAllAndInsert(exchangeRates);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
                IsRunning = false;
                IsEnabled = false;
                return;
            }

            //Consulta Nombres de Tasas
            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://openexchangerates.org");
                var url = "/api/currencies.json";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Error", response.StatusCode.ToString(), "Aceptar");
                    IsRunning = false;
                    IsEnabled = false;
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                nameRates = JsonConvert.DeserializeObject<NameRates>(result);
                //dataService.DeleteAllAndInsert(nameRates);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "Aceptar");
                IsRunning = false;
                IsEnabled = false;
                return;
            }

            LoadRates();
            loadPreferences();
            IsRunning = false;
            IsEnabled = true;
        }

        private void LoadRates()
        {
            Rates.Clear();
            dataService.DeleteAll(new Rate());
            var type = typeof(Rates);
            var properties = type.GetRuntimeFields();
            var nameType = typeof(NameRates);
            var nameProperties = nameType.GetRuntimeFields();
            foreach (var property in properties)
            {
                var code = property.Name.Substring(1, 3);
                foreach (var nameProperty in nameProperties)
                {
                    var nameCode = nameProperty.Name.Substring(1, 3);
                    if (code == nameCode)
                    {
                        Rates.Add(new Rate
                        {
                            Code = code,
                            TaxRate = (double)property.GetValue(exchangeRates.Rates),
                            Name = code + " - " +(string)nameProperty.GetValue(nameRates),

                        });
                        dataService.Insert(new Rate
                        {
                            Code = code,
                            TaxRate = (double)property.GetValue(exchangeRates.Rates),
                            Name = code + " - " + (string)nameProperty.GetValue(nameRates),

                        });
                    }

                }
            }
            
        }

        private void LoadRatesOffline()
        {
            Rates.Clear();
            List<Rate> rates = new List<Rate>();
            rates = dataService.Get<Rate>(false);

            for (int i = 0; i < rates.Count; i++)
            {
                Rates.Add(rates[i]);
                //Consultar Preferencias
                //ConsultarPreferencias(rateList[i]);
            }
            
        }

        private void loadPreferences()
        {
            preference = dataService.First<Preference>(false);
            if (preference != null)
            {
                SourceRate = preference.SourceRate;
                TargetRate = preference.TargetRate;
            }
        }
        #endregion

        #region Commands
        public ICommand ConvertMoneyCommand
        {
            get { return new RelayCommand(ConvertMoney); }
        }

        private async void ConvertMoney()
        {
            if (Amount <= 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes ingresar un valor a convertir", "Aceptar");
                return;
            }

            if (SourceRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda origen", "Aceptar");
                return;
            }

            if (TargetRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda destino", "Aceptar");
                return;
            }

            decimal amountConverted = Amount / (decimal)SourceRate * (decimal)TargetRate;

           dataService.DeleteAllAndInsert(new Preference {
                SourceRate = SourceRate,
                TargetRate = TargetRate,
            });

            Message = string.Format("{0:N2} = {1:N2}", Amount, amountConverted);
        }

        public ICommand ExchangeRatesCommand
        {
            get { return new RelayCommand(ExchangeRates); }
        }

        private async void ExchangeRates()
        {
            if (SourceRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda origen", "Aceptar");
                return;
            }

            if (TargetRate == 0)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Debes seleccionar la moneda destino", "Aceptar");
                return;
            }

            double temporalRate;
            temporalRate = SourceRate;
            SourceRate = TargetRate;
            TargetRate = temporalRate;

        }

        #endregion
    }

}
