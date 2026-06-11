using System.Configuration;
using System.Data;
using System.Windows;
using System.Net.Http;

namespace App.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App_ : Application
{
	public static readonly HttpClient API = new() {
		BaseAddress = new Uri(ConfigurationManager.AppSettings["API"] ?? "https://localhost:8000/api/")
	};
}

