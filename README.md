# API_DigiBook - ASP.NET Core API với Firebase Firestore

API RESTful cho hệ thống quản lý sách điện tử DigiBook, sử dụng ASP.NET Core 6.0 và cơ sở dữ liệu NoSQL Firebase Firestore.

## 🆕 Cập Nhật Mới Nhất (2026)

API đã được **hoàn toàn đồng bộ** với models của DigiBook frontend! 

**Highlights:**
- ✅ Book model với 34 fields (bao gồm SEO, Tiki integration, rich data)
- ✅ User model với quản lý địa chỉ & wishlist
- ✅ Address management endpoints (CRUD)
- ✅ Wishlist management endpoints
- ✅ SEO-friendly URLs với slug
- ✅ View count tracking tự động
- ✅ Tích hợp Gemini AI cho Chatbot tư vấn sách

## 🎯 Design Patterns

API được xây dựng với kiến trúc chuẩn, áp dụng **5 Design Patterns** chính để đảm bảo code clean, dễ bảo trì và mở rộng:

### 1. 📦 Repository Pattern
- **Vị trí:** Thư mục `Repositories/` và `Interfaces/Repositories/`
- **Mục đích:** Tách biệt logic truy xuất dữ liệu (Firebase) khỏi logic nghiệp vụ (Controllers).
- **Chi tiết:** 
  - Sử dụng Generic Repository `FirestoreRepository<T>` làm base.
  - 8 specific repositories: Books, Users, Authors, Categories, Orders, Reviews, Coupons.
- **Lợi ích:** Dễ dàng thay đổi database (ví dụ từ Firebase sang SQL) mà không ảnh hưởng đến Controllers. Dễ dàng mock data để viết Unit Test.

### 2. ⚡ Command Pattern
- **Vị trí:** Thư mục `Commands/`
- **Mục đích:** Đóng gói các yêu cầu (requests) thành các object độc lập.
- **Chi tiết:**
  - `CommandInvoker` đóng vai trò thực thi và quản lý lịch sử.
  - Áp dụng cho các thao tác phức tạp của Order: `CreateOrderCommand`, `UpdateOrderCommand`, `CancelOrderCommand`, `DeleteOrderCommand`.
- **Lợi ích:** Tách logic xử lý phức tạp ra khỏi Controller, hỗ trợ logging chi tiết từng bước, tạo tiền đề cho tính năng Undo/Redo.

### 3. 🔐 Singleton Pattern
- **Vị trí:** Thư mục `Singleton/`
- **Mục đích:** Đảm bảo chỉ có duy nhất một instance của một class được tạo ra trong toàn bộ vòng đời ứng dụng.
- **Chi tiết:** 
  - `LoggerService.Instance` - Service ghi log các hoạt động quan trọng vào Firestore.
  - Áp dụng cơ chế **Double-check locking** để đảm bảo an toàn trong môi trường đa luồng (Thread-safe).
- **Lợi ích:** Tiết kiệm tài nguyên bộ nhớ và số lượng kết nối đến database. Cung cấp một điểm truy cập toàn cục (global access point) để ghi log.

### 4. 🎁 Decorator Pattern
- **Vị trí:** Thư mục `Decorator/`
- **Mục đích:** Bổ sung tính năng động cho đối tượng mà không làm thay đổi cấu trúc của class đó.
- **Chi tiết:**
  - Tích hợp trong hệ thống tính toán giảm giá (Discount & Checkout).
  - Cho phép cộng dồn (stack) nhiều loại giảm giá: `MembershipDiscountDecorator` + `CouponDiscountDecorator` + `BulkPurchaseDiscountDecorator`...
  - Endpoint chính: `POST /api/discount/calculate-checkout`
- **Lợi ích:** Tránh được chuỗi if-else dài dòng khi tính giá. Dễ dàng thêm các loại khuyến mãi mới (ví dụ: Flash Sale, Freeship) mà không cần sửa code cũ.

### 5. 🎯 Strategy Pattern
- **Vị trí:** Thư mục `Strategy/`
- **Mục đích:** Định nghĩa một tập hợp các thuật toán, đóng gói từng thuật toán và làm cho chúng có thể thay thế lẫn nhau.
- **Chi tiết:**
  - Tích hợp trong hệ thống định giá theo hạng thành viên (Membership Pricing).
  - Các chiến lược: `RegularPricingStrategy` (0%), `MemberPricingStrategy` (10%), `WholesalePricingStrategy` (5-25%), `VIPPricingStrategy` (20-30%).
  - `PricingContext` tự động chọn chiến lược dựa trên `membershipTier` của user.
  - Endpoint chính: `POST /api/pricing/calculate-for-user/{userId}`
- **Lợi ích:** Tách biệt thuật toán tính giá khỏi class sử dụng nó. Dễ dàng chuyển đổi chiến lược tính giá ngay trong lúc ứng dụng đang chạy (runtime).

## 📋 Yêu cầu hệ thống

- .NET 6.0 SDK trở lên
- Firebase Project (với Firestore Database đã được kích hoạt)
- Visual Studio 2022 hoặc Visual Studio Code
- (Tùy chọn) Google AI Studio API Key cho tính năng Chatbot

## 🚀 Hướng dẫn cài đặt

### 1. Clone repository

```bash
git clone <repository-url>
cd API_DigiBook
```

### 2. Cài đặt dependencies

```bash
dotnet restore
```

### 3. Cấu hình Firebase & Môi trường

#### Bước 1: Tạo Firebase Project
1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Tạo project mới hoặc sử dụng project có sẵn
3. Vào mục **Firestore Database** và tạo database (chọn chế độ Test mode cho môi trường dev)

#### Bước 2: Lấy Service Account Key
1. Vào **Project Settings** (biểu tượng bánh răng ⚙️) → tab **Service Accounts**
2. Click nút **Generate new private key**
3. Lưu file JSON tải về vào thư mục gốc của project (cùng cấp với file `.sln`)
4. Đổi tên file thành `firebase-credentials.json`

#### Bước 3: Tạo file cấu hình môi trường (.env)
1. Copy file `.env.template` thành `.env`:
   ```bash
   cp .env.template .env
   ```

2. Mở file `.env` và cập nhật các thông tin thực tế:
   ```env
   FIREBASE_PROJECT_ID=your-project-id-here
   FIREBASE_CREDENTIAL_PATH=firebase-credentials.json
   ASPNETCORE_ENVIRONMENT=Development
   GEMINI_API_KEY=your-google-ai-studio-key
   ```

**Lưu ý Bảo mật Quan trọng:** 
- File `.env` và `firebase-credentials.json` đã được cấu hình trong `.gitignore`.
- **TUYỆT ĐỐI KHÔNG** commit các file này lên Github/Gitlab để tránh lộ thông tin bảo mật.

### 4. Chạy ứng dụng

```bash
dotnet run
```

Hoặc nếu sử dụng Visual Studio: Nhấn nút **Play** (hoặc phím F5)

API sẽ khởi động và lắng nghe tại:
- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`
- Swagger UI (Tài liệu API): `https://localhost:7xxx/swagger`

## 📁 Cấu trúc Project chi tiết

```
API_DigiBook/
├── Interfaces/                          # ✨ Tất cả interfaces (Abstraction)
│   ├── Repositories/                    # Repository interfaces (IRepository, IBookRepository...)
│   ├── Services/                        # Service interfaces (IPriceCalculator, IPricingStrategy)
│   └── Commands/                        # Command pattern interfaces (ICommand)
├── Repositories/                        # Data Access Layer (Implementations)
│   ├── FirestoreRepository.cs           # Base generic repository
│   └── ... (BookRepository, UserRepository...)
├── Singleton/                           # Singleton Pattern implementations
│   └── LoggerService.cs                 # Centralized logging
├── Decorator/                           # Decorator Pattern implementations
│   ├── BasePriceCalculator.cs
│   └── Decorators/                      # Các loại giảm giá (Coupon, Membership, Bulk...)
├── Strategy/                            # Strategy Pattern implementations
│   ├── PricingContext.cs
│   └── Strategies/                      # Các chiến lược giá (VIP, Regular, Wholesale...)
├── Commands/                            # Command Pattern implementations
│   ├── CommandInvoker.cs
│   └── Orders/                          # Các lệnh liên quan đến Order
├── Services/                            # External Services
│   └── FirebaseService.cs               # Khởi tạo kết nối Firebase
├── Controllers/                         # API Endpoints (Routing & HTTP Handling)
├── Models/                              # Domain Entities (Data Transfer Objects)
├── Program.cs                           # Application Entry Point & Dependency Injection
├── .env                                 # Environment variables (Local only)
└── firebase-credentials.json            # Firebase credentials (Local only)
```

## 📚 Collections trong Firestore

Hệ thống sử dụng các collections sau trong cơ sở dữ liệu NoSQL Firestore:

1. **`books`**: Quản lý sách (ISBN, title, author, category, price, stock, rating, cover, description...)
2. **`categories`**: Danh mục sách (name, icon, description)
3. **`authors`**: Tác giả (name, bio, avatar)
4. **`users`**: Người dùng (info, role, status, addresses array, wishlistIds array)
5. **`orders`**: Đơn hàng (customer info, items, status, payment details)
6. **`reviews`**: Đánh giá sản phẩm (rating, content, userId, bookId)
7. **`coupons`**: Mã giảm giá (code, discount value, conditions, limits)
8. **`system_logs`**: Nhật ký hệ thống (action, details, status, user)
9. **`ai_models`**: Quản lý các mô hình AI tích hợp
10. **`system_configs`**: Cấu hình hệ thống động

## 🔌 API Endpoints Chính

*(Xem chi tiết đầy đủ tại Swagger UI khi chạy ứng dụng)*

### 📚 Books API
- `GET /api/books` - Lấy danh sách sách (có phân trang, filter)
- `GET /api/books/isbn/{isbn}` - Chi tiết sách theo ISBN
- `GET /api/books/slug/{slug}` - Lấy sách theo URL slug (SEO)
- `GET /api/books/search?title={name}` - Tìm kiếm sách

### 👤 Users API
- `GET /api/users/{id}` - Thông tin người dùng
- `POST /api/users/{userId}/addresses` - Thêm địa chỉ giao hàng
- `POST /api/users/{userId}/wishlist/{bookId}` - Thêm vào danh sách yêu thích

### 🛒 Orders & Checkout API
- `POST /api/orders` - Tạo đơn hàng mới (Sử dụng Command Pattern)
- `POST /api/discount/calculate-checkout` - Tính tổng tiền giỏ hàng với các mã giảm giá (Sử dụng Decorator Pattern)
- `POST /api/pricing/calculate-for-user/{userId}` - Tính giá sản phẩm theo hạng thành viên (Sử dụng Strategy Pattern)

## 🧪 Testing

Sử dụng Swagger UI được tích hợp sẵn để test API dễ dàng:
1. Chạy ứng dụng
2. Mở trình duyệt truy cập: `https://localhost:<port>/swagger`
3. Gọi endpoint `/api/books/test-connection` đầu tiên để đảm bảo kết nối Firebase thành công.

## 🛠️ Công nghệ & Packages Sử Dụng

- **ASP.NET Core 6.0** - Web API Framework
- **FirebaseAdmin** (3.0.0) - Firebase Admin SDK cho .NET
- **Google.Cloud.Firestore** (3.8.0) - Firestore Database Client
- **DotNetEnv** (3.0.0) - Quản lý biến môi trường từ file `.env`
- **Swashbuckle.AspNetCore** (6.5.0) - Tự động tạo Swagger/OpenAPI documentation

## 📝 Chuẩn Response Format

Tất cả các API responses đều tuân theo một định dạng chuẩn để Frontend dễ dàng xử lý:

```json
{
  "success": true,
  "message": "Thao tác thành công",
  "data": {
    // Dữ liệu trả về (object hoặc array)
  },
  "error": null // Chỉ có giá trị khi success = false
}
```

## 🔒 Lưu ý khi Deploy Production

1. **CORS Policy:** Hiện tại API đang mở `AllowAll` cho môi trường Development. Khi deploy, cần cấu hình lại CORS chỉ cho phép domain của Frontend.
2. **Environment Variables:** Không sử dụng file `.env` trên server production. Thay vào đó, cấu hình trực tiếp các biến môi trường trên hosting provider (Azure, AWS, Heroku...).
3. **Firebase Credentials:** Sử dụng các phương pháp bảo mật của cloud provider (như Azure Key Vault, AWS Secrets Manager) để lưu trữ nội dung file JSON, không upload file vật lý lên server public.

---

**Phát triển bởi Team DigiBook 🚀**