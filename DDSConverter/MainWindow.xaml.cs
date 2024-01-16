using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;
using ImageMagick;
using System.Windows.Input;
using System.Reflection;

namespace DDSConverter
{
	public partial class MainWindow : Window
	{
		private List<string> selectedDDSFilePaths = new List<string>();

		public MainWindow()
		{
			InitializeComponent();
			var resultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");
			if (!Directory.Exists(resultDirectory))
			{
				Directory.CreateDirectory(resultDirectory);
			}
		}

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
			}
		}

		private void BtnMinimize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void BtnClose_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void SelectDDSFiles_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "DDS files (*.dds)|*.dds";
			openFileDialog.Multiselect = true;

			if (openFileDialog.ShowDialog() == true)
			{
				selectedDDSFilePaths.Clear();
				selectedDDSFilePaths.AddRange(openFileDialog.FileNames);

				SelectedFilesTextBox.Text = string.Join(Environment.NewLine, selectedDDSFilePaths);
			}
		}

		private void BtnOpenInFolder_Click(object sender, RoutedEventArgs e)
		{
			string resultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");

			if (!Directory.Exists(resultDirectory))
				Directory.CreateDirectory(resultDirectory);

			Process.Start("explorer.exe", resultDirectory);
		}


		private async void ConvertToPNG_Click(object sender, RoutedEventArgs e)
		{
			DisableAllButtons();

			if (selectedDDSFilePaths.Count == 0)
			{
				MessageBox.Show("Выберите файл/файлы DDS.");
				EnableAllButtons();
				return;
			}

			string resultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");

			if (!Directory.Exists(resultDirectory))
			{
				Directory.CreateDirectory(resultDirectory);
			}

			ConversionProgressBar.Value = 0;
			ConversionProgressBar.IsIndeterminate = true;

			try
			{
				await Task.Run(() => ConvertDDSToPNG(resultDirectory));

				ConversionProgressBar.IsIndeterminate = false;
				ConversionProgressBar.Value = 100;
				ConversionStatusText.Text = "Конвертация завершена!";

				MessageBox.Show("Все файлы успешно сконвертированы.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

				selectedDDSFilePaths.Clear();
				SelectedFilesTextBox.Text = string.Empty;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Произошла ошибка при конвертации: {ex.Message}");
			}
			finally
			{
				EnableAllButtons();
				ResetProgressBar();
			}
		}


		private void ResetProgressBar()
		{
			ConversionProgressBar.Value = 0;
			ConversionProgressBar.IsIndeterminate = false;
			ConversionStatusText.Text = string.Empty;
		}

		private void ConvertDDSToPNG(string outputDirectory)
		{
			int totalFiles = selectedDDSFilePaths.Count;
			int processedFiles = 0;

			foreach (string filePath in selectedDDSFilePaths)
			{
				string pngPath = Path.Combine(outputDirectory, Path.ChangeExtension(Path.GetFileName(filePath), ".png"));

				try
				{
					using (MagickImage image = new MagickImage(filePath))
					{
						image.Write(pngPath);
					}

					Debug.WriteLine($"Файл сконвертирован: {pngPath}");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при конвертации файла {filePath}: {ex.Message}");
				}

				processedFiles++;
				double progress = (double)processedFiles / totalFiles * 100;
				Dispatcher.Invoke(() => UpdateProgressBar(progress));
			}
		}

		private void UpdateProgressBar(double value)
		{
			ConversionProgressBar.Value = value;
			ConversionStatusText.Text = $"Конвертация: {value:F2}% завершена";
		}

		private void SaveToArchive_Click(object sender, RoutedEventArgs e)
		{
			string resultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Result");

			if (!Directory.Exists(resultDirectory))
			{
				MessageBox.Show("Директория с PNG-файлами не существует. Пожалуйста, выполните конвертацию DDS в PNG сначала.");
				return;
			}

			var archivePath = Path.Combine(resultDirectory, "ConvertedImages.zip");

			using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
			{
				foreach (var filePath in Directory.EnumerateFiles(resultDirectory, "*.png"))
				{
					archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
				}
			}

			MessageBox.Show($"Результаты сохранены в архив {archivePath}.");
		}

		private void DisableAllButtons()
		{
			btnSelectDDS.IsEnabled = false;
			btnConvertDDS.IsEnabled = false;
			btnSaveToArchive.IsEnabled = false;
		}

		private void EnableAllButtons()
		{
			btnSelectDDS.IsEnabled = true;
			btnConvertDDS.IsEnabled = true;
			btnSaveToArchive.IsEnabled = true;
		}
	}
}
