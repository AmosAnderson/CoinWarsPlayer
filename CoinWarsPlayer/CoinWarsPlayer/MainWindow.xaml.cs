using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;

namespace CoinWarsPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Queue<VideoType> workQueue = new Queue<VideoType>();

        bool playingPromo = false;
        bool playing = false;

        readonly string positiveLocation = "positive";

        List<Uri> positiveVideos = new List<Uri>();

        readonly string negativeLocation = "negative";

        List<Uri> negativeVideos = new List<Uri>();

        readonly string promoLocation = "promo";

        List<Uri> promoVideos = new List<Uri>();

        Random rand = new Random();

        SerialPort port = null;

        public MainWindow()
        {
            InitializeComponent();
            Mouse.OverrideCursor = Cursors.None;

            positiveVideos = GetVideoNames(positiveLocation);
            negativeVideos = GetVideoNames(negativeLocation);
            promoVideos = GetVideoNames(promoLocation);

            StartPromo();

            string portName = SerialPort.GetPortNames().FirstOrDefault();

            if (!string.IsNullOrEmpty(portName))
            {
                port = new SerialPort(portName)
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };

                port.DataReceived += Port_DataReceived;

                port.Open();
            }
        }

        private void AddWorkToQueue(string coin)
        {
            if (!int.TryParse(coin, out int coinValue))
                return;

            if (coinValue == 9999)
                return;

            if (coinValue == 1)
                workQueue.Enqueue(VideoType.Positive);
            else
                workQueue.Enqueue(VideoType.Negative);

            DoWork();
        }

        private void PlayVideo(VideoType type)
        {
            Uri file = null;

            int count = 0;
            int random = 0;

            switch (type)
            {
                case VideoType.Positive:
                    count = positiveVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    file = positiveVideos[random];
                    break;
                case VideoType.Negative:
                    count = negativeVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    file = negativeVideos[random];
                    break;
                case VideoType.Promo:
                    count = promoVideos.Count();
                    if (count <= 0)
                        break;
                    random = rand.Next(count);
                    file = promoVideos[random];
                    break;
                default:
                    return;
            }

            playing = true;
            CoinWarsPlayer.Stop();
            CoinWarsPlayer.Close();
            CoinWarsPlayer.Source = file;
            CoinWarsPlayer.Play();

        }

        private List<Uri> GetVideoNames(string directory)
        {
            var videos = new List<Uri>();

            string videoDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), directory);

            videos = System.IO.Directory.GetFiles(videoDirectory)
                                        .Select(d => new Uri(d))
                                        .ToList();

            return videos;
        }

        private void StartPromo()
        {
            playingPromo = true;

            PlayVideo(VideoType.Promo);
        }

        private void StopPromo()
        {
            if (!playingPromo)
                return;

            CoinWarsPlayer.Stop();
            CoinWarsPlayer.Close();

            playingPromo = false;

        }

        private void DoWork()
        {
            bool hasWork = workQueue.Count() > 0;

            if (!playingPromo && playing)
                return;

            if (hasWork)
            {
                if (playingPromo)
                {
                    StopPromo();
                }

                VideoType work = workQueue.Dequeue();

                PlayVideo(work);

            }
            else
            {
                if (!playingPromo)
                    StartPromo();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    int fileNumber = ((int)e.Key) - 34;
                    AddWorkToQueue(fileNumber.ToString());
                    break;
                case Key.S: // S pressed: stop media playback
                    CoinWarsPlayer.Stop();
                    CoinWarsPlayer.Close();
                    break;
                case Key.P: // P pressed: pause media playback
                    break;
                case Key.Escape:        // close if escape is pressed
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void CoinWarsPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            playing = false;
            playingPromo = false;
            DoWork();
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            SerialPort sp = sender as SerialPort;

            string coin = sp.ReadTo("#");

            if (!string.IsNullOrWhiteSpace(coin))
            {
                Dispatcher.Invoke(() => AddWorkToQueue(coin));
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }
    }

    enum VideoType
    {
        Positive = 1,
        Negative = 2,
        Promo = 3
    }
}
