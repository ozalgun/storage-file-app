# Storage File App

Büyük dosyaların otomatik olarak küçük parçalara (chunk) ayrılması, bu parçaların farklı depolama sağlayıcılarına dağıtılması ve gerektiğinde birleştirilerek dosya bütünlüğünün korunmasının sağlandığı bir .NET Console Application.

## 🏗️ Mimari Yapı

Bu proje **Clean Architecture** ve **Domain-Driven Design** prensiplerine uygun olarak tasarlanmıştır:

```
src/
├── StorageFileApp.Domain/           # Domain entities, interfaces, business logic
├── StorageFileApp.Application/      # Application services, DTOs, use cases
├── StorageFileApp.Infrastructure/  # External concerns (DB, Storage, Logging)
└── StorageFileApp.Console/         # Console application entry point

tests/
├── StorageFileApp.Domain.Tests/
├── StorageFileApp.Application.Tests/
├── StorageFileApp.Infrastructure.Tests/
└── StorageFileApp.Console.Tests/
```

### 📋 Katman Açıklamaları

- **Domain Layer**: Core business logic, entities, value objects, domain services
- **Application Layer**: Use cases, application services, DTOs, interfaces
- **Infrastructure Layer**: Database, external storage, logging, configuration
- **Console Layer**: User interface ve application entry point

## 🚀 Özellikler

- ✅ Büyük dosyaları otomatik chunk'lama
- ✅ Dinamik chunk boyutlandırma algoritması
- ✅ Çoklu storage provider desteği
- ✅ Metadata veritabanında saklama
- ✅ Dosya bütünlük kontrolü (SHA256)
- ✅ Dependency Injection (IoC)
- ✅ Kapsamlı loglama
- ✅ Test edilebilir mimari

## 🛠️ Gereksinimler

- .NET 9.0 SDK
- Visual Studio 2022 / JetBrains Rider / VS Code

## 📦 Kurulum

1. Repository'yi klonlayın:
```bash
git clone <repository-url>
cd storage-file-app
```

2. Solution'ı restore edin:
```bash
dotnet restore
```

3. Projeyi build edin:
```bash
dotnet build
```

4. Testleri çalıştırın:
```bash
dotnet test
```

5. Console uygulamasını çalıştırın:
```bash
dotnet run --project src/StorageFileApp.Console
```

## 🧪 Test

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli test projesini çalıştır
dotnet test tests/StorageFileApp.Domain.Tests/

# Coverage ile test
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Mimari Prensipler

- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Architecture**: Dependencies point inward, Domain is independent
- **Dependency Injection**: Constructor injection, interface-based design
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Specification Pattern**: Complex query logic

## 🔧 Geliştirme

### Yeni Feature Ekleme
1. Domain layer'da entity/interface tanımla
2. Application layer'da service/use case implement et
3. Infrastructure layer'da concrete implementation yaz
4. Console layer'da user interface ekle
5. Test coverage ekle

### Yeni Storage Provider Ekleme
1. `IStorageProvider` interface'ini implement et
2. Infrastructure layer'da concrete class yaz
3. Dependency injection configuration'ı güncelle
4. Test coverage ekle

## 📝 Loglama

Proje Serilog kullanarak kapsamlı loglama yapar:
- File operations
- Chunk operations
- Storage provider operations
- Error handling
- Performance metrics

## 🔒 Güvenlik

- Dosya bütünlük kontrolü (SHA256 checksum)
- Input validation
- Secure file handling
- Access control (gelecekte eklenecek)

## 🚧 Roadmap

- [ ] Web API interface
- [ ] Real-time progress monitoring
- [ ] Compression support
- [ ] Encryption support
- [ ] Cloud storage providers (AWS S3, Azure Blob)
- [ ] Performance optimization
- [ ] Monitoring ve metrics

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 👥 Geliştirici

Storage File App Team

---

**Not**: Bu proje, modern .NET geliştirme pratiklerini, clean architecture prensiplerini ve distributed systems kavramlarını bir araya getiren kapsamlı bir örnek projedir.
