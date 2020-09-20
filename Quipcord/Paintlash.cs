using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace SpongeLake {
    public class Paintlash {
        private DiscordClient client;
        public void OnLoad(Program p) {
            this.client = p.client;
        }
        public async void Handle(MessageCreateEventArgs e) {
            if (e.Author.IsCurrent) {
                return;
            }
            switch (e.Message.Content) {
                case "!paint":
                    Bitmap b = new Bitmap(500, 500);
                    var f = "image.png";
                    b.Save(f);


                    await e.Channel.SendFileAsync(f);
                    File.Delete(f);
                    break;
            }
        }
    }
}
