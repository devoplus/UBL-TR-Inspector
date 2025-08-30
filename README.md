# ubltr-inspector (Devoplus)

Tek komutla UBL-TR belgelerinde **XSD doğrulama** ve **iş kuralı & aritmetik tutarlılık** denetimleri.
Bu proje, `ubltr-validator-cli` ve `ubltr-consistency-checker` fikirlerinin tek bir **CLI** altında birleşmiş hâlidir.

## Özellikler
- XSD doğrulama (schemas klasöründen otomatik yükleme; bulunmazsa kibarca atlanır)
- İş kuralları (örnek): 
  - `SUM-001` Satır toplamları ↔ `LegalMonetaryTotal`
  - `VAT-002` KDV %0 ise `TaxExemptionReasonCode` zorunlu
  - `CUR-001` Para birimi ölçeğine uygun ondalık basamak sayısı
- Profiller: `einvoice`, `earchive`, `edespatch` (örnek profil dosyası)
- Raporlar: **console**, **json**, **markdown**
- CI-dostu: `--fail-on warn|error` çıkış kodları

## Hızlı Başlangıç

```bash
# .NET 8 gerekir
dotnet build ./src/UblTr.Cli/UblTr.Cli.csproj -c Release

# XSD + kurallar
dotnet run --project ./src/UblTr.Cli -- check ./samples/invoice-invalid.xml --profile einvoice --report json:out/report.json --fail-on error

# Sadece XSD
dotnet run --project ./src/UblTr.Cli -- validate ./samples/invoice-valid.xml

# Sadece kurallar
dotnet run --project ./src/UblTr.Cli -- rules ./samples/invoice-invalid.xml --profile einvoice --report md:out/summary.md
```

### Şema dosyaları (XSD)
- Resmî UBL-TR XSD/Schematron dosyalarını **`schemas/`** altına bırakın.
- Örn. `schemas/ubltr-1.0/*.xsd` — araç tüm `.xsd` dosyalarını yükler.
- Şema bulunamazsa XSD doğrulaması **otomatik atlanır** ve raporda bilgi notu görünür.

## Profiller
Örnek profil `rules/profiles/einvoice.json` altında. CLI ile `--profile` verin; `--rules` anahtarı ileride eklenecek.

## Örnekler
- `samples/invoice-valid.xml` — kurallara uygun örnek
- `samples/invoice-invalid.xml` — toplam/istisna ve kur cinsinden **ondalık** hataları içeren örnek

## Lisans
MIT
