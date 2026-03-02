
# Exulofra API - Gerçek Zamanlı Sesli Çeviri ve Dublaj Sunucusu

*Read this in [English](README_EN.md)*

Bu proje, .NET 10 ve Vertical Slice Architecture (VSA) prensipleriyle geliştirdiğim gerçek zamanlı bir ses işleme ve çeviri servisidir.

Sistem, istemcilerden gelen ham mikrofon seslerini SignalR ve MessagePack protokolleri üzerinden alıyor. Ardından Azure Cognitive Services kullanarak bu sesleri anlık işliyor, çeviriyor ve hedeflenen dilde sentezlenmiş sesi Base64 formatında doğrudan istemciye geri gönderiyor.

## Temel Özellikler

* **Gerçek Zamanlı İletişim:** SignalR ve MessagePack ile yüksek performanslı, gecikmesiz binary veri akışı.
* **Dinamik Ses Sentezi:** Çeviriler, seçilen dile ve karaktere uygun seslerle (örneğin en-US-JennyNeural veya tr-TR-AhmetNeural) SSML altyapısıyla hızlandırılarak okunur.
* **Gelişmiş Oturum Modları:**
* **Dubbing (1):** Eşzamanlı çeviri ve hedef dilde sesli dublaj.
* **Reporting (2):** Sadece dikte işlemi. Çeviri ve seslendirme yapılmaz, maliyet ve performans optimize edilir.
* **Dialogue (3):** İki farklı dili konuşan kişilerin tek cihazda karşılıklı sohbet etmesi. Azure Sürekli Dil Algılama ile konuşan kişi otomatik tespit edilir.
* **Broadcast (4):** Sistem sesi veya ekran paylaşımı üzerinden sessiz altyazı üretimi.


* **Güvenlik ve İzolasyon:** JWT Bearer altyapısı ve CreatorUserId sayesinde her kullanıcının oturum ve geçmiş kayıtları tamamen izole edilmiştir. Websocket bağlantıları da token ile korunur.

## Kurulum ve Gereksinimler

Projeyi yerel ortamında çalıştırabilmek için bir Azure hesabına ve Speech Services kaynağına ihtiyacın var. Sistem ağır ses işleme yükünü kendi donanımın yerine Azure bulut altyapısına devreder.

### Azure Ayarları

Azure Portal üzerinden oluşturduğun Speech Services kaynağının API anahtarını ve bölgesini appsettings.json dosyana şu şekilde eklemelisin:

```json
{
  "Azure": {
    "SpeechKey": "SİZİN_AZURE_SPEECH_ANAHTARINIZ",
    "SpeechRegion": "westeurope"
  }
}

```

### Veritabanı ve Çalıştırma

Proje Entity Framework Core kullanıyor. Veritabanını ayağa kaldırmak ve projeyi başlatmak için terminalinde şu komutları çalıştırman yeterli:

```bash
dotnet ef database update
dotnet run

```

## API Dokümantasyonu (Scalar)

Geliştirdiğimiz tüm REST uç noktalarını test edebilmen için projeye Scalar entegre ettik. Uygulamayı başlattıktan sonra tarayıcında şu adrese giderek API'yi detaylıca inceleyebilir ve JWT token alarak testlerini yapabilirsin:

`https://localhost:<PORT>/scalar`

## SignalR Hub Kullanımı

Gerçek zamanlı işlemler için /translation-hub uç noktasını kullanıyoruz. İstemci tarafında entegrasyonu şu adımlarla yapabilirsin:

1. **Bağlantı:** JWT Token, QueryString (?access_token=...) olarak gönderilip bağlantı başlatılır. MessagePack protokolü kullanımı zorunludur.
2. **Katılım:** Invoke("JoinSession", sessionId) komutu ile ilgili odaya girilir.
3. **Akış:** Subject kanalı açılarak StartStream metoduna iletilir ve mikrofondan alınan 16kHz PCM ses verisi akıtılmaya başlanır.
4. **Dinleme:**
* ReceivePartial: Cümle bitmeden anlık akan metin.
* ReceiveTranslation: Cümle bittiğinde oluşan kesin çeviri ve konuşmacı etiketi.
* ReceiveAudio: Hedef dilde sentezlenmiş, anında oynatmaya hazır Base64 formatındaki ses verisi.
