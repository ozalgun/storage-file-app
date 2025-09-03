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

### Core Features
- ✅ **Otomatik Chunk'lama**: Büyük dosyaları dinamik boyutlarda parçalara ayırma
- ✅ **Çoklu Storage Provider**: FileSystem, Database, MinIO S3-compatible storage
- ✅ **Dosya Bütünlük Kontrolü**: SHA256 checksum ile güvenli dosya doğrulama
- ✅ **Metadata Yönetimi**: PostgreSQL'de chunk bilgileri ve dosya metadata'sı
- ✅ **Event-Driven Architecture**: RabbitMQ ile asenkron mesajlaşma
- ✅ **Caching**: Redis ile performans optimizasyonu

### Technical Features
- ✅ **Clean Architecture**: Domain-Driven Design prensiplerine uygun katmanlı yapı
- ✅ **SOLID Principles**: Test edilebilir ve genişletilebilir kod yapısı
- ✅ **Dependency Injection**: Microsoft.Extensions.DependencyInjection ile IoC
- ✅ **Repository Pattern**: Veri erişim katmanı soyutlaması
- ✅ **Unit of Work**: Transaction yönetimi
- ✅ **Comprehensive Logging**: Serilog ile detaylı loglama
- ✅ **Health Monitoring**: Sistem sağlık kontrolü ve monitoring
- ✅ **Docker Support**: Containerized development environment

## 🛠️ Gereksinimler

- .NET 9.0 SDK
- Docker & Docker Compose
- Visual Studio 2022 / JetBrains Rider / VS Code

## 🐳 Docker ile Kurulum ve Çalıştırma

### 1. Repository'yi Klonlayın
```bash
git clone https://github.com/ozalgun/storage-file-app.git
cd storage-file-app
```

### 2. Docker Compose ile Servisleri Başlatın
```bash
# Tüm servisleri başlat (PostgreSQL, RabbitMQ, Redis, MinIO)
docker-compose up -d

# Servislerin durumunu kontrol et
docker-compose ps
```

### 3. Servis Erişim Bilgileri

| Servis | URL | Kullanıcı | Şifre |
|--------|-----|-----------|-------|
| **PostgreSQL** | `localhost:5432` | `storageuser` | `storagepass123` |
| **RabbitMQ Management** | http://localhost:15672 | `storageuser` | `storagepass123` |
| **MinIO Console** | http://localhost:9001 | `minioadmin` | `minioadmin123` |
| **Redis** | `localhost:6379` | - | - |

### 4. Uygulamayı Çalıştırın
```bash
# Solution'ı restore edin
dotnet restore

# Projeyi build edin
dotnet build

# Testleri çalıştırın
dotnet test

# Console uygulamasını çalıştırın
dotnet run --project src/StorageFileApp.Console
```

### 5. Servisleri Durdurma
```bash
# Tüm servisleri durdur
docker-compose down

# Verileri de silmek için
docker-compose down -v
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

### Proje Yapısı
```
src/
├── StorageFileApp.Domain/           # Domain entities, services, events
│   ├── Entities/                   # File, Chunk, StorageProvider entities
│   ├── Services/                   # Domain services (chunking, integrity, validation)
│   ├── Events/                     # Domain events
│   └── ValueObjects/               # FileMetadata, ChunkInfo
├── StorageFileApp.Application/      # Application layer
│   ├── Services/                   # Application services
│   ├── UseCases/                   # Use case interfaces
│   ├── DTOs/                       # Data transfer objects
│   └── Events/                     # Event handlers
├── StorageFileApp.Infrastructure/  # External concerns
│   ├── Repositories/               # Data access implementations
│   ├── Services/                   # Storage providers, messaging
│   └── Data/                       # DbContext, configurations
└── StorageFileApp.Console/         # Console application
    └── Services/                   # Console-specific services
```

### Yeni Feature Ekleme
1. **Domain Layer**: Entity, value object, domain service tanımla
2. **Application Layer**: Use case interface ve application service implement et
3. **Infrastructure Layer**: Repository, external service implementation yaz
4. **Console Layer**: User interface ve menu option ekle
5. **Test Coverage**: Unit testler ve integration testler ekle

### Yeni Storage Provider Ekleme
1. **Interface Implementation**: `IStorageProvider` interface'ini implement et
2. **Infrastructure Service**: `StorageFileApp.Infrastructure/Services/` altında concrete class yaz
3. **Factory Pattern**: `StorageProviderFactory`'ye yeni provider'ı ekle
4. **Dependency Injection**: `InfrastructureServiceCollectionExtensions.cs`'de registration ekle
5. **Configuration**: `appsettings.json`'da provider ayarları ekle
6. **Test Coverage**: Unit testler ve integration testler yaz

### Event-Driven Development
- **Domain Events**: Business logic'te domain event'ler tanımla
- **Event Handlers**: Application layer'da event handler'lar implement et
- **Message Publishing**: RabbitMQ üzerinden event'leri publish et
- **Event Consumers**: Background service'lerde event'leri consume et

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

### Phase 1 - Core Features ✅
- [x] File chunking and merging
- [x] Multiple storage providers
- [x] Database metadata storage
- [x] Event-driven architecture
- [x] Health monitoring
- [x] Docker support

### Phase 2 - Enhanced Features
- [ ] **Web API Interface**: RESTful API endpoints
- [ ] **Real-time Progress**: WebSocket ile progress tracking
- [ ] **Compression Support**: Gzip, Brotli compression
- [ ] **Encryption Support**: AES-256 encryption for sensitive files
- [ ] **Advanced Monitoring**: Prometheus metrics, Grafana dashboards

### Phase 3 - Cloud Integration
- [ ] **AWS S3 Provider**: Native AWS S3 integration
- [ ] **Azure Blob Storage**: Azure Blob Storage provider
- [ ] **Google Cloud Storage**: GCS provider
- [ ] **Multi-region Support**: Cross-region replication

### Phase 4 - Enterprise Features
- [ ] **User Management**: Authentication & authorization
- [ ] **Audit Logging**: Comprehensive audit trails
- [ ] **Backup & Recovery**: Automated backup strategies
- [ ] **Performance Optimization**: Advanced caching, CDN integration
- [ ] **Scalability**: Horizontal scaling support

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 👥 Geliştirici

Özal Algün

---

**Not**: Bu proje, modern .NET geliştirme pratiklerini, clean architecture prensiplerini ve distributed systems kavramlarını bir araya getiren kapsamlı bir örnek projedir.
