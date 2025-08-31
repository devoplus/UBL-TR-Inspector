# UBL-TR-Inspector

Tek komutla UBL-TR belgelerinde XSD doğrulama ve iş kuralı & aritmetik tutarlılık denetimleri yapmanızı sağlar. Kendinize özel iş kurallarını yazmanızı da destekler.

## Özellikler
- XSD doğrulama
- Örnek iş kuralları:
  - `SUM-001` Satır toplamları = `LegalMonetaryTotal` olmalıdır.
  - `VAT-002` KDV %0 ise `TaxExemptionReasonCode` değeri zorunludur.
  - `CUR-001` Para birimi ölçeğine uygun ondalık basamak sayısı kontrolü yapılır.
- Profiller: `einvoice`, `earchive`, `edespatch` (örnek profil dosyası)
- Rapor Formatları: **console**, **json**, **markdown**
- CI-dostu: `--fail-on warn|error` çıkış kodlarını destekler.

## Hızlı Başlangıç

```bash
# .NET 8 gerekir
dotnet build ./src/UblTr.Cli/UblTr.Cli.csproj -c Release

# XSD + iş kuralları
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-invalid.xml --profile einvoice --report json:out/report.json --fail-on error

# Sadece XSD
dotnet run --project ./src/UblTr.Cli -- validate ./samples/invoice-valid.xml

# Sadece iş kuralları
dotnet run --project ./src/UblTr.Cli -- rules ./samples/invoice-invalid.xml --profile einvoice --report md:out/summary.md
```

### Şema dosyaları (XSD)
- Resmi UBL-TR XSD/Schematron dosyalarını **`schemas/`** altına bırakın.
- Örn. `schemas/ubltr-1.0/*.xsd` — araç tüm `.xsd` dosyalarını yükler.
- Şema bulunamazsa XSD doğrulaması **otomatik atlanır** ve raporda bilgi notu görünür.

## Profiller
E-fatura için oluşturulan örnek kontrol profilini `rules/profiles/einvoice.json` altında görebilirsiniz. CLI ile `--profile` argümanını vererek kontrol profilini belirleyebilirsiniz. `--rules` argümanı da ileride eklenecektir.

## Örnekler
- `samples/invoice-valid.xml` — tüm kurallara uygun örnek
- `samples/invoice-invalid.xml` — toplam/istisna ve kur cinsinden **ondalık** hataları içeren örnek

## Kurulum

### Gereksinimler
- .NET 8.0 SDK veya üzeri
- Git (geliştirme için)

### Kurulum Adımları
```bash
# Projeyi klonlayın
git clone https://github.com/username/UBL-TR-Inspector.git
cd UBL-TR-Inspector

# Projeyi derleyin
dotnet build -c Release

# Araç testini çalıştırın
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-valid.xml --profile einvoice
```

### Bağımsız Çalıştırılabilir Dosya Oluşturma
```bash
# Tek dosya olarak yayınlama
dotnet publish ./src/UblTr.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux için
dotnet publish ./src/UblTr.Cli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

## Detaylı Kullanım

### Komut Örnekleri
```bash
# Tüm denetimler (XSD + iş kuralları)
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-invalid.xml --profile einvoice

# JSON rapor oluşturma
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-invalid.xml --profile einvoice --report json:./output/report.json

# Markdown rapor oluşturma
dotnet run --project ./src/UblTr.Cli -- rules ./samples/invoice-invalid.xml --profile einvoice --report md:./output/summary.md

# CI/CD entegrasyonu için çıkış kodları
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-invalid.xml --profile einvoice --fail-on error
echo $LASTEXITCODE  # Windows PowerShell
echo $?            # Linux/macOS
```

### Rapor Formatları
- **console**: Konsol çıktısı (varsayılan)
- **json**: JSON formatında detaylı rapor
- **markdown**: Markdown formatında özet rapor

### Çıkış Kodları
- `0`: Başarılı (hata yok)
- `1`: Uyarı seviyesinde bulgular
- `2`: Hata seviyesinde bulgular

## Geliştirme

### Proje Yapısı
```
src/
├── UblTr.Cli/          # Ana komut satırı uygulaması
├── UblTr.Core/         # Temel modeller ve sınıflar
├── UblTr.Rules/        # İş kuralları motoru
│   └── Rules/          # Mevcut iş kuralları
└── UblTr.Xml/          # XSD doğrulama motoru
```

### Yeni İş Kuralı Ekleme
1. `src/UblTr.Rules/Rules/` klasörü altında yeni kural dosyası oluşturun
2. `IRule` arayüzünü uygulayın
3. Profil dosyasında kuralı etkinleştirin

Örnek kural implementasyonu:
```csharp
public class MyCustomRule : IRule
{
    public string Id => "CUSTOM-001";
    public Severity Severity => Severity.Error;
    
    public IEnumerable<RuleViolation> Validate(XDocument document)
    {
        // Kural mantığınız burada
        yield return new RuleViolation
        {
            Id = Id,
            Severity = Severity,
            Message = "Özel kural ihlali mesajı",
            Line = 0,
            Column = 0
        };
    }
}
```

### Test Etme
```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test projesi
dotnet test ./tests/UblTr.Rules.Tests/
```

## Katkıda Bulunma
1. Bu projeyi fork edin
2. Yeni bir feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

### Katkı Kuralları
- Kod standartlarına uyun
- Testlerinizi yazın
- Commit mesajlarını Türkçe veya İngilizce olarak açık yazın
- Büyük değişiklikler için önce issue oluşturun

## Sık Sorulan Sorular (SSS)

### XSD dosyaları nereden bulabilirim?
Resmi UBL-TR XSD dosyalarını [GİB'in resmi sitesinden](https://www.gib.gov.tr) indirebilirsiniz. İndirdiğiniz dosyaları `schemas/` klasörüne yerleştirin.

### Özel profil nasıl oluştururum?
`rules/profiles/` klasörü altında yeni bir JSON dosyası oluşturun ve hangi kuralların etkin olacağını belirtin:
```json
{
  "name": "my-custom-profile",
  "rules": {
    "enable": ["SUM-*", "CUR-001"],
    "disable": ["VAT-002"]
  }
}
```

### CI/CD entegrasyonu nasıl yapılır?
```yaml
# GitHub Actions örneği
- name: Validate UBL documents
  run: |
    dotnet run --project ./src/UblTr.Cli -- check ./documents/*.xml --profile einvoice --fail-on error
```

## Changelog
- **v1.0.0**: İlk kararlı sürüm
  - XSD doğrulama desteği
  - Temel iş kuralları (SUM-001, VAT-002, CUR-001)
  - JSON ve Markdown rapor formatları
  - Profil desteği

## Lisans
Proje, MIT lisansı ile lisanslanmıştır.