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

	using App.API.Schema;

	/// <summary>
	/// Логика взаимодействия для Admin.xaml
	/// </summary>

	public partial class Admin : Window {
		public Admin() {
			InitializeComponent();

			SearchTextBox.KeyDown += async (s, e) => {
				if (e.Key == Key.Enter)
					if (SearchTextBox.Text.IsWhiteSpace() || SearchTextBox.Text.Length >= 3)
						CatalogueListBox.ItemsSource = await Catalogue.search(SearchTextBox.Text);
			};
		}

		internal async Task load() {
			var orders = async () => {
				try {
					var response = await App_.API.GetAsync("orders");

					if (response.IsSuccessStatusCode) {

						OrdersListBox.ItemsSource = await response.Content.ReadFromJsonAsync<Order[]>();

						if (OrdersListBox.ItemsSource is null)
							MessageBox.Show("Получены неверные данные о заказах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					} else
						MessageBox.Show("Не удалось загрузить заказы.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};

			var _loadOrders = orders();

			var users = async () => {
				try {
					var response = await App_.API.GetAsync("users");

					if (response.IsSuccessStatusCode) {

						UsersListBox.ItemsSource = await response.Content.ReadFromJsonAsync<UserResponse[]>();

						if (UsersListBox.ItemsSource is null)
							MessageBox.Show("Получены неверные данные о пользователях.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					} else
						MessageBox.Show("Не удалось загрузить пользователей.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};

			var _loadUsers = users();

			await _loadOrders;
			await _loadUsers;
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.DataContext is Product product) {
				button.IsEnabled = false;

				var productEditor = new ProductEditor(product);
				productEditor.Closed += async (s, e) => button.IsEnabled = true;
				productEditor.Show();
			}
		}

		private async void Button_Click_1(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.DataContext is Product product) {
				button.IsEnabled = false;

				try {
					var response = await App_.API.DeleteAsync($"products/{product.Id}");

					if (response.IsSuccessStatusCode) {
						CatalogueListBox.ItemsSource = await Catalogue.load();
					} else
						MessageBox.Show("Не удалось удалить продукт.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private async void Button_Click_2(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.DataContext is UserResponse user) {
				button.IsEnabled = false;

				try {
					var response = await App_.API.DeleteAsync($"users/{user.Id}");

					if (response.IsSuccessStatusCode) {
						await load();
					} else
						MessageBox.Show("Не удалось удалить пользователя.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private async void Button_Click_3(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.DataContext is Order order) {
				button.IsEnabled = false;

				try {
					var response = await App_.API.DeleteAsync($"orders/{order.Id}");

					if (response.IsSuccessStatusCode) {
						await load();
					} else
						MessageBox.Show("Не удалось удалить заказ.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (SearchTextBox.Text.IsWhiteSpace() || SearchTextBox.Text.Length >= 3)
				CatalogueListBox.ItemsSource = await Catalogue.search(SearchTextBox.Text);
		}

		private void Window_SourceInitialized(object sender, EventArgs e) {
			this.MinWidth = this.ActualWidth;
			this.MinHeight = this.ActualHeight;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e) {
			CatalogueListBox.ItemsSource = await Catalogue.load();
			await load();
		}

		private async void Button_Click_4(object sender, RoutedEventArgs e) {
			if (sender is Button button && button.DataContext is Order order) {
				var cart = new Cart(order.Products.Select(product => new Product(product.Id, product.Name, product.Count, product.Price, product.Unit, product.Image, product.Description, product.Manufacturer, product.Provider, product.Discount, product.Categories)).ToArray());
				cart.ShowInTaskbar = true;
				cart.Show();
			}
		}
	}
}
