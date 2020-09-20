using DSharpPlus;
using Newtonsoft.Json;
using Quipcord;
using System;
using System.IO;
using System.Threading.Tasks;
namespace SpongeLake {
    public class Program {
        public DiscordClient client;
        public Acrolash acro;
        public Markovlash markov;
        public Paintlash paint;
        public Quiplash quip;
        public Quotelash quote;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            client = new DiscordClient(new DiscordConfiguration {
                Token = "Njg0NTk5MTYyNzA4MTY0NjEx.XmRwWg.O-hdJj2zqk6Ivv571fW2qdDCDCY",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            client.MessageCreated += async e => {
                try {
                    switch (e.Message.Content) {
                        case "!save":
                            PeriodicSave();
                            break;
                        case "!exit":
                            PeriodicSave();
                            Environment.Exit(0);
                            break;
                    }
                    await Task.Run(() => acro.Handle(e));
                    await Task.Run(() => markov.Handle(e));
                    await Task.Run(() => paint.Handle(e));
                    await Task.Run(() => quip.Handle(e));
                    await Task.Run(() => quote.Handle(e));
                } catch(Exception ex) {
                    PeriodicSave();
                    //e.Channel.SendMessageAsync("Oops");
                    throw ex;
                }
            };
            acro = Load<Acrolash>() ?? new Acrolash();
            markov = Load<Markovlash>() ?? new Markovlash();
            paint = Load<Paintlash>() ?? new Paintlash();
            quip = Load<Quiplash>() ?? new Quiplash();
            quote = Load<Quotelash>() ?? new Quotelash();

            acro.OnLoad(this);
            markov.OnLoad(this);
            paint.OnLoad(this);
            quip.OnLoad(this);
            quote.OnLoad(this);
            
            PeriodicSave();
            await client.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Save(acro);
            Save(markov);
            Save(paint);
            Save(quip);
            Save(quote);
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
        public void Save<T>(T t) {
            var f = $"./{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));

            File.WriteAllText(f, JsonConvert.SerializeObject(t, Formatting.Indented, new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            }));
        }
        public T Load<T>() {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f)) {
                T t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
                return t;
            }
            return default(T);
        }
        public bool Load<T>(out T t) {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f)) {
                t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
                return true;
            }
            t = default(T);
            return false;
        }
    }
}
