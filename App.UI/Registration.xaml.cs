namespace App.UI {
	using System;
	using System.Net.Http;
	using System.Net.Http.Json;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using System.Text;

	using API.Schema;

	/// <summary>
	/// Логика взаимодействия для Registration.xaml
	/// </summary>
	public partial class Registration : Window {
		public Registration() {
			InitializeComponent();

			LoginTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) register(); };
			PasswordTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) register(); };
			ConfirmPasswordTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) register(); };
			NameTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) register(); };
		}

		private async void RegisterButton_Click(object sender, RoutedEventArgs e) {
			await register();
		}

		private async Task register() {
			string login = LoginTextBox.Text;
			string password = PasswordTextBox.Password;
			string password2 = ConfirmPasswordTextBox.Password;
			string name = NameTextBox.Text;

			LoginTextBox.Clear();
			PasswordTextBox.Clear();
			ConfirmPasswordTextBox.Clear();
			NameTextBox.Clear();

			if (!RegisterButton.IsEnabled)
				return;

			if (login.IsWhiteSpace() || password.IsWhiteSpace() || password2.IsWhiteSpace()) {
				MessageBox.Show("Не все поля заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			if (password != password2) {
				MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			RegisterButton.IsEnabled = false;

			try {
				var request = new UserCreateRequest(
					Login: login,
					Password: password,
					Name: name.IsWhiteSpace() ? null : name,
					Role: "Client"
				);

				var response = await App_.API.PostAsJsonAsync("auth/register", request);

				if (response.IsSuccessStatusCode) {
					var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

					if (result is not null) {
						NameTextBox.Clear();

						App_.API.DefaultRequestHeaders.Authorization =
							new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

						await Profile.load();
						new Main().Show();
						Close();
					} else
						MessageBox.Show("Получены неверные данные для авторизации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				} else
					MessageBox.Show("Не удалось зарегистрироваться.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
			} catch (HttpRequestException) {
				MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} finally {
				RegisterButton.IsEnabled = true;
			}
		}

		private void Hyperlink_BackToLogin(object sender, RoutedEventArgs e) {
			new Login().Show();
			Close();
		}
	}
}