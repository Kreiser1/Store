namespace App.UI {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;
	using System.Windows.Threading;

	internal class ProductEqualityComparer : IEqualityComparer<Product> {
		public bool Equals(Product x, Product y) => x.Id == y.Id;
		public int GetHashCode(Product obj) => obj.Id.GetHashCode();
	}

	public partial class Cart : Window {
		public static HashSet<Product> Products = new(new ProductEqualityComparer());

		public Cart() {
			InitializeComponent();
			Update();
		}

		public void Update() {
			CartListBox.ItemsSource = Products.ToArray();
			CartListBox.InvalidateVisual();
		}

		private async void OrderButton_Click(object sender, RoutedEventArgs e) {
			
        }

		private async void ClearButton_Click(object sender, RoutedEventArgs e) {
			Products.Clear();
			Update();
		}

		internal void Window_CancelClosing(object sender, CancelEventArgs e) {
			e.Cancel = true;
			Hide();
		}
	}
}
