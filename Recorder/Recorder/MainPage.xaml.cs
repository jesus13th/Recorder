using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Threading.Tasks;

using MediaManager;

using Plugin.AudioRecorder;

using Xamarin.Essentials;
using Xamarin.Forms;
using Android.Widget;
using Xamarin.CommunityToolkit.Extensions;

namespace Recorder {
    public partial class MainPage : ContentPage {
        private AudioRecorderService audioRecorder = new AudioRecorderService();
        private ObservableCollection<string> audios = new ObservableCollection<string>();
        private string path;
        private string newFile;

        private Timer timer;
        private DateTime startTime;

        public MainPage() {
            InitializeComponent();
            BindingContext = this;
            audioRecorder.StopRecordingOnSilence = audioRecorder.StopRecordingAfterTimeout = false;
            path = Path.Combine("/storage/emulated/0/", "Recorders");
            LoadFiles();
        }
        private void LoadFiles() {
            audios.Clear();
            foreach (var dir in Directory.GetFiles(path))
                audios.Add(dir.Replace(path, string.Empty));

            AudioList.ItemsSource = audios;
        }
        private void t_Tick(object sender, EventArgs e) {
            var cTimer = (startTime - DateTime.Now).ToString("hh':'mm':'ss' '");
            MainThread.BeginInvokeOnMainThread(() => timeLabel.Text = cTimer);
        }
        public void StartCountDownTimer() {
            timer = new Timer(1000);
            startTime = DateTime.Now;
            timer.Elapsed += t_Tick;
            timer.Start();
        }
        private async void recordBtn_Clicked(object sender, EventArgs e) {
            try {
                if (!audioRecorder.IsRecording) {
                    audioRecorder.FilePath = newFile = Path.Combine(path, $"Audio_Recorder_{ DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") }.wav");
                    recordBtn.IsEnabled = false;
                    await audioRecorder.StartRecording();
                    StartCountDownTimer();
                    recordBtn.IsEnabled = true;
                    recordBtn.ImageSource = "mic_on.png";
                    await this.DisplayToastAsync("Inicia la grabacion", 2000);
                } else {
                    timer.Stop();
                    timeLabel.Text = "00:00:00";
                    recordBtn.IsEnabled = false;
                    await audioRecorder.StopRecording();
                    recordBtn.IsEnabled = true;
                    recordBtn.ImageSource = "mic_off.png";
                    audios.Add(newFile.Replace(path, string.Empty));
                    AudioList.ItemsSource = audios;
                    await this.DisplayToastAsync("Fin la grabacion", 2000);
                }
            } catch (Exception ex) {
                await DisplayAlert("Error", ex.Message, "ok");
            }
        }
        private async void audioItemButton_Clicked(object sender, EventArgs e) {
            string item = (string)(sender as Xamarin.Forms.ImageButton).CommandParameter;
            string action = await DisplayActionSheet("Opciones", "Salir", null, "Cambiar Nombre", "Compartir", "Eliminar");
            Debug.WriteLine(action);
            switch (action) {
                case "Cambiar Nombre": ChangeNameItem(item); break;
                case "Eliminar": DeleteAudio(item); break;
                case "Compartir": await ShareAudio(item); break;
            }
        }
        private async void ChangeNameItem(string item) {
            File.Move($"{path}/{item}", $"{path}/{await DisplayPromptAsync("Cambiar nombre", "Introduzca en nuevo nombre para el archivo", "Confirmar", "Cancelar")}.wav");
            LoadFiles();
        }
        private async void DeleteAudio(string item) {
            if (await DisplayAlert("Eliminar Audio", $"Estas seguro de eliminar el audio: {item}?", "Eliminar", "Cancelar")) {
                File.Delete($"{path}/{item}");
                audios.Remove(item);
                AudioList.ItemsSource = audios;
            }
        }
        private async Task ShareAudio(string item) {
            var file = $"{path}/{item}";
            await Share.RequestAsync(new ShareFileRequest($"Compartir {item}", new ShareFile(file)));
        }
        private async void AudioList_ItemTapped(object sender, ItemTappedEventArgs e) => await CrossMediaManager.Current.Play($"{path}/{e.Item}");
    }
}