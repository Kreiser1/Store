namespace App.UI {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net.Http;
	using System.Net.Http.Json;
	using System.Text;
	using System.Text.Json;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;
	using System.Windows.Threading;

	using App.API.Schema;

	public partial class Cart : Window {
		public static HashSet<Product> Products = new();

		public Cart(Product[]? products = null) {
			InitializeComponent();

			if (products is not null) {
				CartListBox.ItemsSource = products;
				ClearButton.IsEnabled = false;
				OrderButton.IsEnabled = false;

				int cost = 0;
				foreach (var product in products)
					cost += product.Count * (product.Discount is not null ? (int)MathF.Round(product.Price - product.Price * (product.Discount.Value / 100f)) : product.Price);
				CostTextBox.Text = $"{cost} руб.";
			} else
				Update();
		}

		public void Update() {
			CartListBox.ItemsSource = Products.ToArray();
			int cost = 0;
			foreach (var product in Products)
				cost += product.Count * (product.Discount is not null ? (int)MathF.Round(product.Price - product.Price * (product.Discount.Value / 100f)) : product.Price);
			CostTextBox.Text = $"{cost} руб.";
		}

		private async void OrderButton_Click(object sender, RoutedEventArgs e) {
			var request = new OrderCreateRequest(Profile.Id.Value, Products.Select(product => new OrderProduct(product.Id, product.Count)).ToArray());

			OrderButton.IsEnabled = false;

			try {
				var response = await App_.API.PostAsJsonAsync("orders", request, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });

				if (response.IsSuccessStatusCode) {
					Products.Clear();
					Update();
					Close();
				} else
					MessageBox.Show("Не удалось сделать заказ.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
			} catch (HttpRequestException) {
				MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} finally {
				OrderButton.IsEnabled = true;
			}
		}

		private async void ClearButton_Click(object sender, RoutedEventArgs e) {
			Products.Clear();
			Update();
		}

		internal void Window_CancelClosing(object sender, CancelEventArgs e) {
			e.Cancel = true;
			Hide();
		}

		private void Window_SourceInitialized(object sender, EventArgs e) {
			this.MinWidth = this.ActualWidth;
			this.MinHeight = this.ActualHeight;
		}
	}
}
