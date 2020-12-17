using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Common.Common;
namespace Postcard {
    public class Program {
        public DiscordClient client;
        public Postcard c;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            client = new DiscordClient(new DiscordConfiguration {
                Token = "Nzg2NzMwMzMyMzA0NTcyNDI4.X9Kptw.eTaTl9kFETGPuzDroY-VHRALdBU",
                TokenType = TokenType.Bot,
            });
            client.MessageCreated += async (client, e) => {
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
                    await Task.Run(() => c.Handle(e));
                } catch (Exception ex) {
                    PeriodicSave();
                    //e.Channel.SendMessageAsync("Oops");
                    throw ex;
                }
            };
            c = Load<Postcard>() ?? new Postcard();

            c.OnLoad(this);

            PeriodicSave();
            await client.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Save(c);
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
    }
    public class Postcard {
        DiscordClient client;
        Dictionary<ulong, Session> sessions;
        public Postcard() {
            sessions = new Dictionary<ulong, Session>();
        }
        public void OnLoad(Program p) {
            this.client = p.client;
        }
        public async Task Handle(MessageCreateEventArgs m) {
            foreach(var line in m.Message.Content.Split("\n")) {

                switch (line) {
                    case string postcard when postcard.StartsWith("!postcard", out string command):
                        switch (command) {
                            case string create when create.StartsWith("create", out string args): {
                                    Bitmap b;

                                    var images = m.Message.Attachments.FirstOrDefault(a => a.Width > 0);
                                    if (images != null) {
                                        System.Net.WebRequest request =
                                        System.Net.WebRequest.Create(m.Author.AvatarUrl);
                                        System.Net.WebResponse response = request.GetResponse();
                                        System.IO.Stream responseStream = response.GetResponseStream();

                                        b = new Bitmap(responseStream);
                                    } else {
                                        b = new Bitmap(600, 600);
                                    }
                                    var f = $"{Path.GetFullPath(".")}/Postcard";
                                    /*
                                    if (!Directory.Exists(f)) {
                                        Directory.CreateDirectory(f);
                                    }
                                    */
                                    f = $"{f}/postcard_{Directory.GetFiles(f, "*.*").Count().ToString().PadLeft(4, '0')}.png";
                                    Console.WriteLine(f);

                                    var s = sessions[m.Guild.Id] = new Session() {
                                        file = f,
                                        image = b,
                                        points = new Dictionary<Point, Participant>()
                                    };

                                    int i = 0;
                                    var interval = 64;
                                    for (int y = interval; y < b.Height; y += interval) {
                                        s.points[new Point(64 + (i % 2 == 0 ? 0 : interval), y)] = null;
                                        i++;
                                    }
                                    for (int y = interval; y < b.Height; y += interval) {
                                        s.points[new Point(b.Width - 64 - (i % 2 == 0 ? 0 : interval), y)] = null;
                                        i++;
                                    }
                                    break;
                                }
                            case string add when add.StartsWith("add", out string args): {
                                    if (!sessions.TryGetValue(m.Guild.Id, out var s)) {
                                        return;
                                    }

                                    List<DiscordUser> users = new List<DiscordUser>();
                                    if (m.Message.MentionedUsers.Any()) {
                                        foreach (var u in m.Message.MentionedUsers) {
                                            users.Add(await client.GetUserAsync(u.Id));
                                        }
                                    } else {
                                        users.Add(m.Author);
                                    }
                                    foreach (var user in users) {
                                        var open = s.points.Keys.Where(p => s.points[p] == null);
                                        if (!open.Any()) {
                                            return;
                                        }
                                        var pos = open.First();

                                        System.Net.WebRequest request =
                                            System.Net.WebRequest.Create(user.AvatarUrl);
                                        System.Net.WebResponse response = request.GetResponse();
                                        System.IO.Stream responseStream = response.GetResponseStream();

                                        s.points[pos] = new Participant() {
                                            avatar = new Bitmap(responseStream),
                                            name = user.Username,
                                            status = user.Presence?.Activity?.CustomStatus?.Name ?? ""
                                        };
                                    }
                                    break;
                                }
                            case string join when join.StartsWith("join", out string args): {
                                    if (!sessions.TryGetValue(m.Guild.Id, out var s)) {
                                        return;
                                    }

                                    var user = m.Author;
                                    var open = s.points.Keys.Where(p => s.points[p] == null);
                                    if (!open.Any()) {
                                        return;
                                    }
                                    var pos = open.First();

                                    System.Net.WebRequest request =
                                        System.Net.WebRequest.Create(user.AvatarUrl);
                                    System.Net.WebResponse response = request.GetResponse();
                                    System.IO.Stream responseStream = response.GetResponseStream();

                                    s.points[pos] = new Participant() {
                                        avatar = new Bitmap(responseStream),
                                        name = user.Username,
                                        status = user.Presence?.Activity?.CustomStatus?.Name ?? ""
                                    };
                                    break;
                                }
                            case string print when print.StartsWith("print"): {
                                    if (!sessions.TryGetValue(m.Guild.Id, out var s)) {
                                        return;
                                    }

                                    var b = s.image.Clone() as Bitmap;
                                    using (Graphics g = Graphics.FromImage(b)) {
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                                        foreach ((var pos, var participant) in s.points) {
                                            if (participant == null) {
                                                continue;
                                            }
                                            var name = participant.name;
                                            var a = participant.avatar;

                                            int width = 32;
                                            int height = 32;
                                            g.DrawString(name, new Font("Consolas", 12), new SolidBrush(Color.White), new Point(pos.X - 10 * name.Length / 2, pos.Y - height - 18));
                                            g.DrawImage(participant.avatar, pos.X - width / 2, pos.Y - height, width, height);
                                        }
                                    }
                                    b.Save(s.file);
                                    await m.Channel.SendFileAsync(s.file);

                                    break;
                                }
                        }
                        break;
                }
            }
        }

        public class Participant {
            public Image avatar;
            public string name;
            public string status;
        }
        public class Session {
            public Bitmap image;
            public string file;
            public string message;
            public Dictionary<Point, Participant> points;
        }
    }
}
