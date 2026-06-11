namespace App.UI {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
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

	using App.API.Schema;

	/// <summary>
	/// Логика взаимодействия для ProfileEditor.xaml
	/// </summary>
	public partial class ProfileEditor : Window {
		public ProfileEditor() {
			InitializeComponent();
		
			LoginTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await editProfile(); };
			OldPasswordTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await editProfile(); };
			NewPasswordTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await editProfile(); };
			NameTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await editProfile(); };
		}

		internal void Window_CancelClosing(object sender, CancelEventArgs e) {
			e.Cancel = true;
			Hide();
		}

		private async Task editProfile() {
			string login = LoginTextBox.Text;
			string oldPassword = OldPasswordTextBox.Password;
			string? newPassword = NewPasswordTextBox.Password.IsWhiteSpace() ? null : NewPasswordTextBox.Password;
			string? name = NameTextBox.Text.IsWhiteSpace() ? null : NameTextBox.Text;

			LoginTextBox.Clear();
			OldPasswordTextBox.Clear();
			NewPasswordTextBox.Clear();
			NameTextBox.Clear();

			if (!ConfirmButton.IsEnabled)
				return;

			if (login.IsWhiteSpace() || oldPassword.IsWhiteSpace()) {
				MessageBox.Show("Не все обязательные поля заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			ConfirmButton.IsEnabled = false;

			try {
				var request = new UserUpdateRequest(
					login,
					newPassword,
					oldPassword,
					null, name
				);

				var response = await App_.API.PatchAsJsonAsync("users/me", request);

				if (response.IsSuccessStatusCode) {
					await Profile.load();
					Hide();
				} else
					MessageBox.Show("Не удалось изменить профиль.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
			} catch (HttpRequestException) {
				MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} finally {
				ConfirmButton.IsEnabled = true;
			}
		}

		private async void ConfirmButton_Click(object sender, RoutedEventArgs e) {
			await editProfile();
		}
	}
}
