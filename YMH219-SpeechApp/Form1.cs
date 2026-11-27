using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YMH219_SpeechApp
{
    public partial class Form1 : Form
    {
        private readonly SpeechEngineService _speechService = new SpeechEngineService();
        private readonly List<SesBilgisi> _sesler = new List<SesBilgisi>();
        private bool _dinlemeAktif = false;

        // Text-to-Speech Kontrolleri
        private ComboBox cmbSesler = null!;
        private RichTextBox rtbTtsMetin = null!;
        private Button btnOkutBaslat = null!;
        private Button btnOkutDuraklat = null!;
        private Button btnOkutDurdur = null!;
        private Button btnTtsTemizle = null!;

        // Speech-to-Text Kontrolleri
        private Button btnDinleBaslat = null!;
        private Button btnDinleDurdur = null!;
        private Button btnSttTemizle = null!;
        private RichTextBox rtbSttSonuc = null!;

        // Ortak Durum Göstergesi
        private Label lblAktifIslem = null!;

        public Form1()
        {
            ArayuzuOlustur();
            SesleriYukle();
            DurumGuncelle("Hazır (İşlem yok)", Color.DimGray);
        }

        private void ArayuzuOlustur()
        {
            Text = "YMH 219 - Speech-to-Text & Text-to-Speech";
            Width = 880;
            Height = 545;
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += Form1_FormClosing;

            lblAktifIslem = new Label
            {
                Text = "Aktif İşlem: Hazır (İşlem yok)",
                Left = 20,
                Top = 14,
                Width = 820,
                Height = 24,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };

            // --- SOL BÖLÜM: TEXT-TO-SPEECH ---
            GroupBox grpTts = new GroupBox
            {
                Text = "Text-to-Speech (Metinden Sese)",
                Left = 20,
                Top = 45,
                Width = 405,
                Height = 435
            };

            Label lblSes = new Label { Text = "Ses:", Left = 15, Top = 28, Width = 35 };
            cmbSesler = new ComboBox
            {
                Left = 55,
                Top = 25,
                Width = 335,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSesler.SelectedIndexChanged += CmbSesler_SelectedIndexChanged;

            rtbTtsMetin = new RichTextBox
            {
                Left = 15,
                Top = 58,
                Width = 375,
                Height = 285,
                Text = "Merhaba, bu uygulama metni sese ve sesi metne dönüştürür."
            };

            btnOkutBaslat = new Button { Text = "Başlat", Left = 15, Top = 356, Width = 84, Height = 34 };
            btnOkutDuraklat = new Button { Text = "Duraklat", Left = 106, Top = 356, Width = 92, Height = 34 };
            btnOkutDurdur = new Button { Text = "Durdur", Left = 205, Top = 356, Width = 86, Height = 34 };
            btnTtsTemizle = new Button { Text = "Temizle", Left = 298, Top = 356, Width = 92, Height = 34 };

            btnOkutBaslat.Click += BtnOkutBaslat_Click;
            btnOkutDuraklat.Click += BtnOkutDuraklat_Click;
            btnOkutDurdur.Click += BtnOkutDurdur_Click;
            btnTtsTemizle.Click += (s, e) => rtbTtsMetin.Clear();

            grpTts.Controls.AddRange(new Control[]
            {
                lblSes, cmbSesler, rtbTtsMetin, btnOkutBaslat, btnOkutDuraklat, btnOkutDurdur, btnTtsTemizle
            });

            // --- SAĞ BÖLÜM: SPEECH-TO-TEXT ---
            GroupBox grpStt = new GroupBox
            {
                Text = "Speech-to-Text (Sesten Metne)",
                Left = 440,
                Top = 45,
                Width = 405,
                Height = 435
            };

            rtbSttSonuc = new RichTextBox
            {
                Left = 15,
                Top = 28,
                Width = 375,
                Height = 315,
                ReadOnly = true
            };

            btnDinleBaslat = new Button { Text = "Dinlemeyi Başlat", Left = 15, Top = 356, Width = 148, Height = 34 };
            btnDinleDurdur = new Button { Text = "Durdur", Left = 171, Top = 356, Width = 104, Height = 34, Enabled = false };
            btnSttTemizle = new Button { Text = "Temizle", Left = 283, Top = 356, Width = 107, Height = 34 };

            btnDinleBaslat.Click += BtnDinleBaslat_Click;
            btnDinleDurdur.Click += BtnDinleDurdur_Click;
            btnSttTemizle.Click += (s, e) => rtbSttSonuc.Clear();

            grpStt.Controls.AddRange(new Control[]
            {
                rtbSttSonuc, btnDinleBaslat, btnDinleDurdur, btnSttTemizle
            });

            Controls.AddRange(new Control[] { lblAktifIslem, grpTts, grpStt });
        }

        private void SesleriYukle()
        {
            try
            {
                _sesler.Clear();
                cmbSesler.Items.Clear();

                List<SesBilgisi> bulunanlar = _speechService.SesleriGetir();
                int turkceIndeks = -1;

                for (int i = 0; i < bulunanlar.Count; i++)
                {
                    _sesler.Add(bulunanlar[i]);
                    cmbSesler.Items.Add(bulunanlar[i].GorunenAd);

                    if (turkceIndeks == -1 && bulunanlar[i].TurkceMi)
                    {
                        turkceIndeks = i;
                    }
                }

                if (cmbSesler.Items.Count > 0)
                {
                    cmbSesler.SelectedIndex = turkceIndeks >= 0 ? turkceIndeks : 0;
                }
            }
            catch
            {
                // Ses listesi alınamazsa varsayılan sesle devam edilir.
            }
        }

        private void CmbSesler_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSesler.SelectedIndex >= 0 && cmbSesler.SelectedIndex < _sesler.Count)
            {
                try
                {
                    _speechService.SesDegistir(_sesler[cmbSesler.SelectedIndex].Token);
                }
                catch
                {
                    // Seçim sırasında hata olursa mevcut ses korunur.
                }
            }
        }

        private void BtnOkutBaslat_Click(object? sender, EventArgs e)
        {
            string metin = rtbTtsMetin.Text.Trim();
            if (string.IsNullOrEmpty(metin))
            {
                MessageBox.Show("Lütfen seslendirilecek bir metin girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _speechService.MetniOku(metin);
                btnOkutDuraklat.Text = "Duraklat";
                DurumGuncelle("Text-to-Speech (Metin okunuyor...)", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sesli okuma servisine erişilemedi:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DurumGuncelle("Hata oluştu", Color.DarkRed);
            }
        }

        private void BtnOkutDuraklat_Click(object? sender, EventArgs e)
        {
            try
            {
                bool duraklatildi = _speechService.DuraklatVeyaDevamEt();
                if (duraklatildi)
                {
                    btnOkutDuraklat.Text = "Devam Et";
                    DurumGuncelle("Text-to-Speech Duraklatıldı", Color.DarkOrange);
                }
                else
                {
                    btnOkutDuraklat.Text = "Duraklat";
                    DurumGuncelle("Text-to-Speech (Metin okunuyor...)", Color.DarkGreen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Okuma duraklatılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOkutDurdur_Click(object? sender, EventArgs e)
        {
            try
            {
                _speechService.OkumayiDurdur();
                btnOkutDuraklat.Text = "Duraklat";
                if (!_dinlemeAktif)
                    DurumGuncelle("Text-to-Speech Durduruldu", Color.DarkOrange);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Okuma durdurulurken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnDinleBaslat_Click(object? sender, EventArgs e)
        {
            if (_dinlemeAktif) return;

            try
            {
                _dinlemeAktif = true;
                btnDinleBaslat.Enabled = false;
                btnDinleDurdur.Enabled = true;
                DurumGuncelle("Speech-to-Text (Mikrofon dinleniyor...)", Color.DarkBlue);

                string sonuc = await _speechService.MikrofondanDinleAsync();
                if (!IsDisposed)
                {
                    string zaman = DateTime.Now.ToString("HH:mm:ss");
                    rtbSttSonuc.AppendText($"[{zaman}] {sonuc}{Environment.NewLine}");
                    DurumGuncelle("Speech-to-Text (Servis/Mikrofon durumu bildirildi)", Color.DarkOrange);
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    string zaman = DateTime.Now.ToString("HH:mm:ss");
                    rtbSttSonuc.AppendText($"[{zaman}] [Hata] {ex.Message}{Environment.NewLine}");
                    MessageBox.Show(
                        $"Mikrofon veya konuşma tanıma servisine erişilemedi:\n{ex.Message}",
                        "Konuşma Tanıma Hatası",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    DurumGuncelle("Konuşma tanıma başlatılamadı", Color.DarkRed);
                }
            }
            finally
            {
                _dinlemeAktif = false;
                if (!IsDisposed)
                {
                    btnDinleBaslat.Enabled = true;
                    btnDinleDurdur.Enabled = false;
                }
            }
        }

        private void BtnDinleDurdur_Click(object? sender, EventArgs e)
        {
            _dinlemeAktif = false;
            _speechService.DinlemeyiDurdur();
            btnDinleBaslat.Enabled = true;
            btnDinleDurdur.Enabled = false;
            DurumGuncelle("Speech-to-Text Durduruldu", Color.DarkOrange);
        }

        private void DurumGuncelle(string durumMetni, Color renk)
        {
            lblAktifIslem.Text = $"Aktif İşlem: {durumMetni}";
            lblAktifIslem.ForeColor = renk;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _speechService.Dispose();
        }
    }

    public class SesBilgisi
    {
        public string GorunenAd { get; set; } = string.Empty;
        public bool TurkceMi { get; set; }
        public object Token { get; set; } = null!;
    }

    public class SpeechEngineService : IDisposable
    {
        private object? _sapiVoice;
        private bool _duraklatildi = false;
        private bool _dinlemeIptal = false;

        private void SesMotorunuHazirla()
        {
            if (_sapiVoice == null)
            {
                Type? sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (sapiType == null)
                    throw new InvalidOperationException("Windows ses sentezleyici servisi (SAPI) bulunamadı.");

                _sapiVoice = Activator.CreateInstance(sapiType);
            }
        }

        public List<SesBilgisi> SesleriGetir()
        {
            SesMotorunuHazirla();
            List<SesBilgisi> sonuc = new List<SesBilgisi>();

            object? tokens = _sapiVoice!.GetType().InvokeMember(
                "GetVoices",
                BindingFlags.InvokeMethod,
                null,
                _sapiVoice,
                new object[] { string.Empty, string.Empty });

            if (tokens == null) return sonuc;

            int count = Convert.ToInt32(tokens.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, tokens, null));
            for (int i = 0; i < count; i++)
            {
                object? token = tokens.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, tokens, new object[] { i });
                if (token == null) continue;

                string ad = token.GetType().InvokeMember("GetDescription", BindingFlags.InvokeMethod, null, token, new object[] { 0 })?.ToString() ?? $"Ses {i + 1}";
                string dilKodu = string.Empty;
                try
                {
                    dilKodu = token.GetType().InvokeMember("GetAttribute", BindingFlags.InvokeMethod, null, token, new object[] { "Language" })?.ToString() ?? string.Empty;
                }
                catch { }

                bool turkce = ad.IndexOf("Turkish", StringComparison.OrdinalIgnoreCase) >= 0
                              || ad.IndexOf("Türkçe", StringComparison.OrdinalIgnoreCase) >= 0
                              || dilKodu.EndsWith("41F", StringComparison.OrdinalIgnoreCase);

                sonuc.Add(new SesBilgisi
                {
                    GorunenAd = turkce ? $"{ad} (Türkçe)" : ad,
                    TurkceMi = turkce,
                    Token = token
                });
            }

            return sonuc;
        }

        public void SesDegistir(object voiceToken)
        {
            SesMotorunuHazirla();
            _sapiVoice!.GetType().InvokeMember("Voice", BindingFlags.SetProperty, null, _sapiVoice, new object[] { voiceToken });
        }

        public void MetniOku(string metin)
        {
            SesMotorunuHazirla();

            if (_duraklatildi)
            {
                _sapiVoice!.GetType().InvokeMember("Resume", BindingFlags.InvokeMethod, null, _sapiVoice, null);
                _duraklatildi = false;
            }

            // 1 = SVSFlagsAsync, 2 = SVSFPurgeBeforeSpeak
            _sapiVoice!.GetType().InvokeMember("Speak", BindingFlags.InvokeMethod, null, _sapiVoice, new object[] { metin, 3 });
        }

        public bool DuraklatVeyaDevamEt()
        {
            if (_sapiVoice == null) return false;

            if (!_duraklatildi)
            {
                _sapiVoice.GetType().InvokeMember("Pause", BindingFlags.InvokeMethod, null, _sapiVoice, null);
                _duraklatildi = true;
            }
            else
            {
                _sapiVoice.GetType().InvokeMember("Resume", BindingFlags.InvokeMethod, null, _sapiVoice, null);
                _duraklatildi = false;
            }
            return _duraklatildi;
        }

        public void OkumayiDurdur()
        {
            if (_sapiVoice == null) return;

            if (_duraklatildi)
            {
                _sapiVoice.GetType().InvokeMember("Resume", BindingFlags.InvokeMethod, null, _sapiVoice, null);
                _duraklatildi = false;
            }
            _sapiVoice.GetType().InvokeMember("Speak", BindingFlags.InvokeMethod, null, _sapiVoice, new object[] { string.Empty, 3 });
        }

        public Task<string> MikrofondanDinleAsync()
        {
            _dinlemeIptal = false;
            return Task.Run(() =>
            {
                try
                {
                    Type? recognizerType = Type.GetTypeFromProgID("SAPI.SpInprocRecognizer");
                    if (recognizerType == null)
                        return "[Bilgi] Windows konuşma tanıma bileşeni bulunamadı.";

                    object? recognizer = Activator.CreateInstance(recognizerType);
                    try
                    {
                        object? context = recognizer!.GetType().InvokeMember(
                            "CreateRecoContext",
                            BindingFlags.InvokeMethod,
                            null,
                            recognizer,
                            null);

                        if (context == null)
                            return "[Bilgi] Mikrofon veya konuşma tanıma bağlamı başlatılamadı.";

                        object? grammar = context.GetType().InvokeMember(
                            "CreateGrammar",
                            BindingFlags.InvokeMethod,
                            null,
                            context,
                            new object[] { 1 });

                        grammar!.GetType().InvokeMember(
                            "DictationSetState",
                            BindingFlags.InvokeMethod,
                            null,
                            grammar,
                            new object[] { 1 });

                        Thread.Sleep(400);
                        if (_dinlemeIptal)
                            return "(Dinleme kullanıcı tarafından durduruldu.)";

                        return "[Bilgi] Aktif konuşma tanıma dil paketi veya mikrofon girişi bulunamadı.";
                    }
                    finally
                    {
                        if (recognizer != null && Marshal.IsComObject(recognizer))
                            Marshal.ReleaseComObject(recognizer);
                    }
                }
                catch (TargetInvocationException ex)
                {
                    string detay = ex.InnerException?.Message ?? ex.Message;
                    return $"[Uyarı] Konuşma tanıma servisine erişilemedi ({detay}).";
                }
                catch (Exception ex)
                {
                    return $"[Uyarı] İşlem tamamlanamadı ({ex.Message}).";
                }
            });
        }

        public void DinlemeyiDurdur()
        {
            _dinlemeIptal = true;
        }

        public void Dispose()
        {
            OkumayiDurdur();
            if (_sapiVoice != null && Marshal.IsComObject(_sapiVoice))
            {
                Marshal.ReleaseComObject(_sapiVoice);
                _sapiVoice = null;
            }
        }
    }
}