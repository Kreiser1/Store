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
using System.Windows.Navigation;
using System.Windows.Shapes;

using App.API.Schema;
using App.Database.Models;

using Microsoft.VisualBasic;

namespace App.UI;

public record Product : ProductResponse {

	public Product(int Id, string Name, int Count, int Price, string? Unit, byte[]? Image, string? Description, string? Manufacturer, string? Provider, float? Discount, CategoryResponse[]? Categories) : base(Id, Name, Count, Price, Unit, Image ?? Catalogue.Placeholder, Description, Manufacturer, Provider, Discount, Categories) { }

	public virtual bool Equals(Product? other) => other is not null && Id == other.Id;

	public override int GetHashCode() => Id.GetHashCode();

	public int RealPrice => Discount is not null ? (int)MathF.Round(Price - Price * (Discount.Value / 100f)) : Price;
};

public record Order : OrderResponse {
	public int TotalCost {
		get {
			int cost = 0;
			foreach (var product in Products)
				cost += product.Count * product.Price;
			return cost;
		}
	}
	public Order(int Id, int UserId, ProductResponse[] Products) : base(Id, UserId, Products) { }
};

public static class Catalogue {
	public static readonly byte[] Placeholder;

	static Catalogue() {
		Uri resource = new Uri("pack://application:,,,/Placeholder.png", UriKind.RelativeOrAbsolute);

		var streamInfo = Application.GetResourceStream(resource);

		if (streamInfo is null)
			Placeholder = [];

		using MemoryStream ms = new MemoryStream();

		streamInfo.Stream.CopyTo(ms);
		Placeholder = ms.ToArray();
	}

	public static Product[]? Products { get; private set; }

	internal static async Task<Product[]?> load() {
		Products = null;

		try {
			var response = await App_.API.GetAsync("products");

			if (response.IsSuccessStatusCode) {
				Products = await response.Content.ReadFromJsonAsync<Product[]>();

				if (Products is null)
					MessageBox.Show("Получены некорректные данные каталога.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} else
				MessageBox.Show("Не удалось загрузить каталог.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
		} catch (HttpRequestException) {
			MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		return Products;
	}

	internal static async Task<Product[]?> search(string? query = null) {
		if (query is null || query.IsWhiteSpace())
			return await load();

		Products = null;

		try {
			var request = new ProductQueryRequest(
				Name: query, Description: query,
				Manufacturer: query, Provider: query);

			var response = await App_.API.PostAsJsonAsync("products/query", request);

			if (response.IsSuccessStatusCode) {
				Products = await response.Content.ReadFromJsonAsync<Product[]>();

				if (Products is null)
					MessageBox.Show("Получены некорректные данные каталога.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} else
				MessageBox.Show("Не удалось загрузить каталог.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
		} catch (HttpRequestException) {
			MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		return Products;
	}

	internal static async Task<Product[]?> filter(string? provider = null) {
		if (provider is null || provider.IsWhiteSpace())
			return await load();

		Products = null;

		try {
			var request = new ProductQueryRequest(Provider: provider);

			var response = await App_.API.PostAsJsonAsync("products/query", request);

			if (response.IsSuccessStatusCode) {
				Products = await response.Content.ReadFromJsonAsync<Product[]>();

				if (Products is null)
					MessageBox.Show("Получены некорректные данные каталога.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} else
				MessageBox.Show("Не удалось загрузить каталог.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
		} catch (HttpRequestException) {
			MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		return Products;
	}
}

public partial class Main : Window {
	internal ProfileEditor profileEditor = new();
	internal Cart cart = new();

	public Main() {
		InitializeComponent();

		profileEditor.IsVisibleChanged += async (s, e) => { if (!(bool)e.NewValue) UsernameTextBox.Text = Profile.Name ?? "..."; };

		SearchTextBox.KeyDown += async (s, e) => {
			if (e.Key == Key.Enter)
				if (SearchTextBox.Text.IsWhiteSpace() || SearchTextBox.Text.Length >= 3) {
					CatalogueListBox.ItemsSource = await Catalogue.search(SearchTextBox.Text);
					ProvidersCombobox.ItemsSource = Catalogue.Products.Select(product => product.Provider).Append("").Distinct().ToArray();
				}
		};

		cart.Closing += async (s, e) => await loadOrders();
	}

	protected override void OnClosed(EventArgs e) {
		base.OnClosed(e);

		profileEditor.Closing -= profileEditor.Window_CancelClosing;
		profileEditor.Close();

		cart.Closing -= cart.Window_CancelClosing;
		cart.Close();
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e) {
		UsernameTextBox.Text = Profile.Name ?? "Гость";

		if (Profile.Id is null)
			EditProfileButton.Visibility = Visibility.Collapsed;

		if (Profile.Role != Role.Admin) {
			AddButton.Visibility = Visibility.Collapsed;
			AdminButton.Visibility = Visibility.Collapsed;
		}

		if (new[] { Role.Admin, Role.Manager }.Contains(Profile.Role)) {
			RefreshButton.Visibility = Visibility.Collapsed;
		} else {
			SearchTextBox.Visibility = Visibility.Collapsed;
			ProvidersCombobox.Visibility = Visibility.Collapsed;
			SortButton.Visibility = Visibility.Collapsed;
		}

		CatalogueListBox.ItemsSource = await Catalogue.load();
		ProvidersCombobox.ItemsSource = Catalogue.Products.Select(product => product.Provider).Append("").Distinct().ToArray();

		await loadOrders();
	}

	private void AddToCartButton_Click(object sender, RoutedEventArgs e) {
		if (sender is Button button)
			if (button.DataContext is Product product) {
				if (Profile.Id is null) {
					new Login().Show();
					Close();
					return;
				}

				int count;

				if (!int.TryParse(Interaction.InputBox("Введите количество продуктов:", "Добавление в корзину"), out count) || count <= 0) {
					MessageBox.Show("Введено неверное количество продуктов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
					return;
				}

				if (count > product.Count) {
					MessageBox.Show("Такого количества продуктов нет в наличии.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
					return;
				}

				Product cartProduct = product with { Count = count };

				if (!Cart.Products.Add(cartProduct))
					MessageBox.Show("Продукт уже есть в корзине.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
				else
					cart.Update();
			}
	}

	private void EditProfileButton_Click(object sender, RoutedEventArgs e) {
		profileEditor.Show();
	}

	private async Task loadOrders() {
		if (Profile.Id is null)
			return;

		try {
			var response = await App_.API.GetAsync("orders/mine");

			if (response.IsSuccessStatusCode) {
				OrdersListBox.ItemsSource = await response.Content.ReadFromJsonAsync<Order[]>();

				if (OrdersListBox.ItemsSource is null)
					MessageBox.Show("Получены неверные данные о заказах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			} else
				MessageBox.Show("Не удалось загрузить заказы.", response.StatusCode.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
		} catch (HttpRequestException) {
			MessageBox.Show("Не удалось подключиться к серверу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private async void CartButton_Click(object sender, RoutedEventArgs e) {
		if (Profile.Id is null) {
			new Login().Show();
			Close();
			return;
		}

		cart.Show();
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e) {
		if (!RefreshButton.IsEnabled)
			return;

		RefreshButton.IsEnabled = false;

		CatalogueListBox.ItemsSource = await Catalogue.load();

		RefreshButton.IsEnabled = true;
	}

	private async void AddButton_Click(object sender, RoutedEventArgs e) {
		AddButton.IsEnabled = false;

		var productEditor = new ProductEditor();
		productEditor.Closed += async (s, e) => AddButton.IsEnabled = true;
		productEditor.Show();
	}

	private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (SearchTextBox.Text.IsWhiteSpace() || SearchTextBox.Text.Length >= 3)
			CatalogueListBox.ItemsSource = await Catalogue.search(SearchTextBox.Text);
	}

	private void Window_SourceInitialized(object sender, EventArgs e) {
		this.MinWidth = this.ActualWidth;
		this.MinHeight = this.ActualHeight;
	}

	private async void AdminButton_Click(object sender, RoutedEventArgs e) {
		AdminButton.IsEnabled = false;

		var admin = new Admin();
		admin.Closed += async (s, e) => AdminButton.IsEnabled = true;
		admin.Show();
	}

	private async void ProvidersCombobox_Selected(object sender, SelectionChangedEventArgs e) {
		CatalogueListBox.ItemsSource = await Catalogue.filter(ProvidersCombobox.SelectedValue.ToString());
	}

	private bool sortMode = false;

	private async void SortButton_Click(object sender, RoutedEventArgs e) {
		sortMode = !sortMode;

		Array.Sort(Catalogue.Products, (x, y) => (sortMode ? x.Count.CompareTo(y.Count) : y.Count.CompareTo(x.Count)));
		CatalogueListBox.ItemsSource = Catalogue.Products.ToArray();
	}
}