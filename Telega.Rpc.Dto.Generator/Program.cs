using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.Rpc.Dto.Generator.Generation;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator
{
    static class Program
    {
        // 17.01.2020 Updated to Layer 122
        // https://github.com/telegramdesktop/tdesktop/commits/master/Telegram/Resources/tl

        private const string MtProtoSchemeUrl = "https://raw.githubusercontent.com/telegramdesktop/tdesktop/master/Telegram/Resources/tl/mtproto.tl";
        private const string ApiSchemeUrl = "https://raw.githubusercontent.com/telegramdesktop/tdesktop/master/Telegram/Resources/tl/api.tl";

        static string DownloadLatestTelegramScheme()
        {
            using var webClient = new WebClient();
            
            string mtprotoScheme = webClient.DownloadString(MtProtoSchemeUrl);
            string apiScheme = webClient.DownloadString(ApiSchemeUrl);

            string telegramScheme = mtprotoScheme + Environment.NewLine + "---types---" + Environment.NewLine + apiScheme;

            int startSchemeIndex = telegramScheme.IndexOf("///////////////////////////////\n/// Authorization key creation", 
                StringComparison.InvariantCulture);

            // Remove needless core types section
            if (startSchemeIndex != -1)
                telegramScheme = telegramScheme.Substring(startSchemeIndex);

            return telegramScheme;
        }

    static async Task Main()
        {
            string rawScheme = DownloadLatestTelegramScheme();

            var scheme = TgSchemeParser.Parse(rawScheme)
                .Apply(SomeExt.ToSome).Apply(TgSchemePatcher.Patch)
                .Apply(SomeExt.ToSome).Apply(TgSchemeNormalizer.Normalize);
            var files = Gen.GenTypes(scheme).Concat(Gen.GenFunctions(scheme)).Concat(new[] { Gen.GenSchemeInfo(scheme) });

            FileSync.Clear();
            foreach (var file in files.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)) await FileSync.Sync(file);
        }
    }
}
