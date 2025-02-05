using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;

namespace ProductsWpf
{
    public partial class MainWindow : MetroWindow
    {
        string connectionString = "Server=localhost; Database=Store; Integrated Security=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

        public ObservableCollection<Product> Products { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Products = new ObservableCollection<Product>();
            LoadProducts();
            DataContext = this;

            RenderOptions.SetBitmapScalingMode(ProductImage, BitmapScalingMode.HighQuality);
            RenderOptions.SetCachingHint(ProductImage, CachingHint.Cache);
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id, name, price, quantity, picture_path FROM Product";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                var name = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                                var price = reader.IsDBNull(2) ? 0.0 : reader.GetDouble(2);
                                var quantity = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                                var picturePath = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

                                Products.Add(new Product
                                {
                                    Id = id,
                                    Name = name,
                                    Price = price,
                                    Quantity = quantity,
                                    PicturePath = picturePath
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }

        private async void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductDataGrid.SelectedItem is Product selectedProduct)
            {
                var message = $"Вы купили: {selectedProduct.Name}\nЦена: {selectedProduct.Price} ₴\nКоличество: {selectedProduct.Quantity}";

                var metroWindow = (MetroWindow)Application.Current.MainWindow;
                await metroWindow.ShowMessageAsync("Покупка", message, MessageDialogStyle.Affirmative);
            }
            else
            {
                var metroWindow = (MetroWindow)Application.Current.MainWindow;
                await metroWindow.ShowMessageAsync("Ошибка", "Выберите товар перед покупкой!", MessageDialogStyle.Affirmative);
            }
        }

        private readonly Dictionary<string, BitmapImage> _imageCache = new();

        private async void ProductDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs e)
        {
            if (ProductDataGrid.SelectedItem is Product selectedProduct)
            {
                ProductNameLabel.Text = selectedProduct.Name;
                ProductPriceLabel.Text = $"Цена: {selectedProduct.Price} ₴";
                ProductQuantityLabel.Text = $"Количество: {selectedProduct.Quantity}";

                if (!string.IsNullOrEmpty(selectedProduct.PicturePath) && File.Exists(selectedProduct.PicturePath))
                {
                    if (_imageCache.TryGetValue(selectedProduct.PicturePath, out var cachedImage))
                    {
                        ProductImage.Source = cachedImage;
                    }
                    else
                    {
                        var image = await LoadImageAsync(selectedProduct.PicturePath);
                        if (image != null)
                        {
                            _imageCache[selectedProduct.PicturePath] = image;
                            ProductImage.Source = image;
                        }
                    }
                }
                else
                {
                    ProductImage.Source = null;
                }
            }
        }

        private async Task<BitmapImage?> LoadImageAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            });
        }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string PicturePath { get; set; } = string.Empty;
    }
}