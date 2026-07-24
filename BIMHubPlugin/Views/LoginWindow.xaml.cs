using System;
using System.Windows;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.Views
{
    public partial class LoginWindow : Window
    {
        private readonly string _apiBaseUrl;

        /// <summary>Результат успешного логина — заполнен, если DialogResult == true.</summary>
        public LoginResult Result { get; private set; }

        public LoginWindow(string apiBaseUrl)
        {
            InitializeComponent();
            _apiBaseUrl = apiBaseUrl;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            LoginButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;

            try
            {
                var result = await AuthApiClient.LoginAsync(_apiBaseUrl, username, password);

                TokenStore.Save(new CachedToken
                {
                    ApiBaseUrl = _apiBaseUrl,
                    Token = result.Token,
                    Username = result.Username,
                    DisplayName = result.DisplayName
                });

                Result = result;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
