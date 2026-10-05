using System;
using System.Windows;
using System.Threading;
using BIMHubPlugin.Models;
using BIMHubPlugin.Services;

namespace BIMHubPlugin.Views
{
    public partial class LoginWindow : Window
    {
        private readonly string _apiBaseUrl;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _closed;

        /// <summary>Результат успешного логина — заполнен, если DialogResult == true.</summary>
        public LoginResult Result { get; private set; }

        public LoginWindow(string apiBaseUrl)
        {
            InitializeComponent();
            _apiBaseUrl = apiBaseUrl;
            Closed += (_, __) => { _closed = true; _lifetime.Cancel(); _lifetime.Dispose(); PasswordBox.Clear(); };
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LoginButton.IsEnabled || _closed) return;
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
                var result = await AuthApiClient.LoginAsync(_apiBaseUrl, username, password, _lifetime.Token);
                if (_closed) return;

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
            catch (OperationCanceledException) when (_closed) { }
            catch (Exception ex)
            {
                if (!_closed) ShowError(ex.Message);
            }
            finally
            {
                if (!_closed) LoginButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
