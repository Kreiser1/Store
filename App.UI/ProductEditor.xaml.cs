namespace App.UI {
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
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

	using Microsoft.Win32;

	/// <summary>
	/// Логика взаимодействия для ProductEditor.xaml
	/// </summary>
	public partial class ProductEditor : Window {
		internal byte[] Image = Catalogue.Placeholder;
		private int? productId;

		public ProductEditor(Product? product = null) {
			InitializeComponent();

			if (product is not null) {
				NameTextBox.Text = product.Name;
				PriceTextBox.Text = product.Price.ToString();
				CountTextBox.Text = product.Count.ToString();
				UnitTextBox.Text = product.Unit ?? "";
				Image = product.Image ?? Image;
				DescriptionTextBox.Text = product.Description ?? "";
				ManufacturerTextBox.Text = product.Manufacturer ?? "";
				ProviderTextBox.Text = product.Provider ?? "";
				DiscountTextBox.Text = product.Discount.ToString();
				productId = product.Id;
			}

			ProductImage.DataContext = new { Image };

			NameTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			PriceTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			CountTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			UnitTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			DescriptionTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			ManufacturerTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			ProviderTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
			DiscountTextBox.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await finish(); };
		}

		private async void AttachImageButton_Click(object sender, RoutedEventArgs e) {
			var openFileDialog = new OpenFileDialog {
				Filter = "Картинки (*.jpg; *.jpeg; *.png; *.bmp; *.gif)|*.jpg; *.jpeg; *.png; *.bmp; *.gif",
				Title = "Выберите картинку...",
				RestoreDirectory = true,
				CheckFileExists = true
			};

			if (openFileDialog.ShowDialog() == true) {
				try {
					Image = File.ReadAllBytes(openFileDialog.FileName);
					ProductImage.DataContext = new { Image };
				} catch {
					MessageBox.Show("Не удалось прикрепить картинку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					Image = Catalogue.Placeholder;
				}
			}
		}

		private async Task finish() {
			int count, price;
			float? discount;

			if (NameTextBox.Text.IsWhiteSpace()) {
				MessageBox.Show("Имя не указано.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			if (!int.TryParse(CountTextBox.Text, out count) || count <= 0) {
				MessageBox.Show("Количество должно быть целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			if (!int.TryParse(PriceTextBox.Text, out price) || count <= 0) {
				MessageBox.Show("Цена должна быть целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}

			if (!DiscountTextBox.Text.IsWhiteSpace())
				if (float.TryParse(DiscountTextBox.Text, out float _discount))
					discount = _discount;
				else {
					MessageBox.Show("Скидка должна быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
					return;
				}
			else
				discount = null;

			FinishButton.IsEnabled = false;

			if (productId is not null) {
				var request = new ProductUpdateRequest(
						  NameTextBox.Text, count, price,
						  UnitTextBox.Text.IsWhiteSpace() ? null : UnitTextBox.Text,
						  DescriptionTextBox.Text.IsWhiteSpace() ? null : DescriptionTextBox.Text,
						  null, Image,
						  ManufacturerTextBox.Text.IsWhiteSpace() ? null : ManufacturerTextBox.Text,
						  ProviderTextBox.Text.IsWhiteSpace() ? null : ProviderTextBox.Text,
						  discount);

				try {
					var response = await App_.API.PatchAsJsonAsync($"products/{productId}", request);

					if (response.IsSuccessStatusCode)
						Close();
					else
						MessageBox.Show("Не удалось изменить продукт.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				} finally {
					FinishButton.IsEnabled = true;
				}
			} else {
				var request = new ProductCreateRequest(
					NameTextBox.Text, count, price,
					UnitTextBox.Text.IsWhiteSpace() ? null : UnitTextBox.Text,
					DescriptionTextBox.Text.IsWhiteSpace() ? null : DescriptionTextBox.Text,
					null, Image,
					ManufacturerTextBox.Text.IsWhiteSpace() ? null : ManufacturerTextBox.Text,
					ProviderTextBox.Text.IsWhiteSpace() ? null : ProviderTextBox.Text,
					discount);

				try {
					var response = await App_.API.PostAsJsonAsync("products", request);

					if (response.IsSuccessStatusCode)
						Close();
					else
						MessageBox.Show("Не удалось добавить продукт.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
				} catch (HttpRequestException) {
					MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				} finally {
					FinishButton.IsEnabled = true;
				}
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e) {
			this.MinWidth = this.ActualWidth;
			this.MinHeight = this.ActualHeight;
		}

		private async void FinishButton_Click(object sender, RoutedEventArgs e) {
			await finish();
		}
	}
}
