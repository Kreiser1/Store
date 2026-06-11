namespace App.UI {
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Net.Http.Json;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;

	using API.Schema;

	/// <summary>
	/// Логика взаимодействия для Login.xaml
	/// </summary>

	public static class Profile {
		public static int? Id { get; private set; }
		public static string? Name { get; private set; }
		public static string? Role { get; private set; }
		internal static async Task load() {
			(Id, Name, Role) = (null, null, null);

			try {
				var response = await App_.API.GetAsync("users/me");

				if (response.IsSuccessStatusCode) {
					var result = await response.Content.ReadFromJsonAsync<UserResponse>();

					if (result is not null) {
						Id = result.Id;
						Name = result.Name;
						Role = result.Role;
					} else
						MessageBox.Show("Получены некорректные данные профиля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				} else
					MessageBox.Show("Не удалось загрузить профиль.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
			} catch (HttpRequestException) {
				MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	};

	public partial class Login : Window {
		public Login() {
			InitializeComponent();

			LoginTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await login(); };
			PasswordTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await login(); };
		}

		private async Task login() {
			var request = new LoginRequest(LoginTextBox.Text, PasswordTextBox.Password);

			LoginTextBox.Clear();
			PasswordTextBox.Clear();

			if (!LoginButton.IsEnabled)
				return;

			LoginButton.IsEnabled = false;

			try {
				var response = await App_.API.PostAsJsonAsync("auth/login", request);

				if (response.IsSuccessStatusCode) {
					var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

					if (result is not null) {
						App_.API.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

						await Profile.load();
						new Main().Show();
						Close();
					} else
						MessageBox.Show("Получены некорректные данные для авторизации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				} else
					MessageBox.Show("Неверный логин или пароль.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
			} catch (HttpRequestException) {
				MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} finally {
				LoginButton.IsEnabled = true;
			}
		}

		private async void LoginButton_Click(object sender, RoutedEventArgs e) {
			await login();
		}

		private void Hyperlink_LoginAsGuest(object sender, RoutedEventArgs e) {
			new Main().Show();
			Close();
		}

		private void Hyperlink_Register(object sender, RoutedEventArgs e) {
			new Registration().Show();
			Close();
		}
	}
}
