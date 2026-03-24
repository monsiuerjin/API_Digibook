# API_DigiBook - ASP.NET Core API với Firebase Firestore

API RESTful cho hệ thống quản lý sách điện tử DigiBook, sử dụng ASP.NET Core 6.0 và Firebase Firestore.

## 🆕 Cập Nhật Mới Nhất (2024)

API đã được **hoàn toàn đồng bộ** với models của DigiBook frontend! 

**Highlights:**
- ✅ Book model với 34 fields (bao gồm SEO, Tiki integration, rich data)
- ✅ User model với quản lý địa chỉ & wishlist
- ✅ Address management endpoints (CRUD)
- ✅ Wishlist management endpoints
- ✅ SEO-friendly URLs với slug
- ✅ View count tracking tự động

## 🎯 Design Patterns

API sử dụng **5 Design Patterns** chính:

### 1. 📦 Repository Pattern
- **Location:** `Repositories/`
- Tách biệt data access logic khỏi business logic
- 8 repositories cho: Books, Users, Authors, Categories, Orders, Reviews, Coupons
- Sử dụng trong tất cả Controllers

### 2. ⚡ Command Pattern
- **Location:** `Commands/`
- Encapsulate operations thành objects
- Hỗ trợ command history & logging
- Sử dụng cho Order operations (Create, Update, Cancel, Delete)

### 3. 🔐 Singleton Pattern
- **Location:** `Singleton/`
- `LoggerService.Instance` - logging service duy nhất cho toàn app
- Thread-safe với double-check locking
- Sử dụng trong nhiều controllers

### 4. 🎁 Decorator Pattern
- **Location:** `Decorator/`
- **Integration:** Tích hợp với Checkout & Coupon System
- **Use Cases:**
  - Stack multiple discounts: Membership + Coupon + Seasonal promotion
  - 6 loại decorators: Coupon, Percentage, Fixed Amount, Membership, Bulk Purchase, Seasonal
  - Endpoint: `POST /api/discount/calculate-checkout` - Calculate total với stacked discounts
  - Support Firestore coupons integration
- **Real Application:** Frontend checkout page có thể call API để tính tổng tiền với nhiều loại giảm giá khác nhau (member discount + coupon + promotional sale)

### 5. 🎯 Strategy Pattern
- **Location:** `Strategy/`
- **Integration:** Tích hợp với User Membership System
- **Use Cases:**
  - Auto-select pricing strategy dựa trên `membershipTier` của user
  - 4 strategies: Regular (0%), Member (10%), Wholesale (5-25%), VIP (20-30%)
  - Endpoint: `POST /api/pricing/calculate-for-user/{userId}` - Calculate giá dựa trên membership
  - Endpoint: `GET /api/pricing/membership/{userId}` - Get thông tin membership & benefits
- **Real Application:** Frontend checkout process có thể call API để show giá theo membership tier của user

## 📋 Yêu cầu

- .NET 6.0 SDK
- Firebase Project
- Visual Studio 2022 hoặc VS Code

## 🚀 Cài đặt

### 1. Clone repository

```bash
git clone <repository-url>
cd API_DigiBook
```

### 2. Cài đặt dependencies

```bash
dotnet restore
```

### 3. Cấu hình Firebase

#### Bước 1: Tạo Firebase Project
1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Tạo project mới hoặc sử dụng project có sẵn
3. Tạo Firestore Database (nếu chưa có)

#### Bước 2: Lấy Service Account Key
1. Vào **Project Settings** (⚙️) → **Service Accounts**
2. Click **Generate new private key**
3. Lưu file JSON vào thư mục gốc của project
4. Đổi tên file thành `firebase-credentials.json`

#### Bước 3: Tạo file .env
1. Copy file `.env.template` thành `.env`:
   ```bash
   cp .env.template .env
   ```

2. Mở file `.env` và cập nhật thông tin:
   ```env
   FIREBASE_PROJECT_ID=your-project-id-here
   FIREBASE_CREDENTIAL_PATH=firebase-credentials.json
   ASPNETCORE_ENVIRONMENT=Development
  GEMINI_API_KEY=your-google-ai-studio-key
   ```

3. Chatbot backend (`POST /api/chat/recommend`) sẽ tự đọc API key theo thứ tự ưu tiên:
  - `Chatbot:GeminiApiKey` (từ config binding)
  - `GEMINI_API_KEY` (biến môi trường)

**Lưu ý:** 
- File `.env` và `firebase-credentials.json` đã được thêm vào `.gitignore` để bảo mật
- KHÔNG commit các file này lên Git

### 4. Chạy ứng dụng

```bash
dotnet run
```

Hoặc sử dụng Visual Studio: nhấn F5

API sẽ chạy tại:
- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`
- Swagger UI: `https://localhost:7xxx/swagger`

## 📁 Cấu trúc Project

```
API_DigiBook/
├── Interfaces/                          # ✨ Tất cả interfaces
│   ├── Repositories/                    # Repository interfaces
│   │   ├── IRepository.cs
│   │   ├── IBookRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── ... (8 interfaces)
│   ├── Services/                        # Service interfaces
│   │   ├── IPriceCalculator.cs
│   │   └── IPricingStrategy.cs         # Strategy pattern interface
│   └── Commands/                        # Command pattern interfaces
│       └── ICommand.cs
├── Repositories/                        # Repository Implementations
│   ├── FirestoreRepository.cs
│   ├── BookRepository.cs
│   ├── UserRepository.cs
│   └── ... (8 repositories)
├── Singleton/                           # Singleton Pattern
│   └── LoggerService.cs
├── Decorator/                           # Decorator Pattern
│   ├── BasePriceCalculator.cs
│   └── Decorators/
│       ├── DiscountDecorator.cs
│       ├── CouponDiscountDecorator.cs
│       └── ... (6 decorators)
├── Strategy/                            # Strategy Pattern
│   ├── PricingContext.cs
│   └── Strategies/
│       ├── RegularPricingStrategy.cs
│       ├── MemberPricingStrategy.cs
│       ├── WholesalePricingStrategy.cs
│       └── VIPPricingStrategy.cs
├── Commands/                            # Command Pattern
│   ├── CommandInvoker.cs
│   └── Orders/
│       ├── CreateOrderCommand.cs
│       └── ... (5 commands)
├── Services/
│   └── FirebaseService.cs
├── Controllers/                         # API Controllers (10 controllers)
├── Models/                              # Data Models (11 models)
├── Program.cs                           # Entry point
├── .env                                 # Environment variables (không commit)
└── firebase-credentials.json            # Firebase credentials (không commit)
```

## 🏗️ Kiến trúc - Repository Pattern

Project sử dụng **Repository Pattern** để tách biệt logic truy cập dữ liệu:

### Lợi ích:
- ✅ Tách biệt concerns (Separation of Concerns)
- ✅ Dễ dàng unit testing (mock repositories)
- ✅ Code sạch và dễ bảo trì
- ✅ Tái sử dụng code
- ✅ Dễ dàng thay đổi data source

### Cấu trúc:
```
IRepository<T>                    # Generic interface
    └── FirestoreRepository<T>    # Base implementation
        └── Specific Repositories  # BookRepository, UserRepository, etc.
```

### Ví dụ sử dụng:

```csharp
// In Controller
public class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;
    
    public BooksController(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllBooks()
    {
        var books = await _bookRepository.GetAllAsync();
        return Ok(books);
    }
}
```

### Repository Methods:

#### Generic Methods (tất cả repositories):
- `GetAllAsync()` - Lấy tất cả
- `GetByIdAsync(id)` - Lấy theo ID
- `AddAsync(entity, customId?)` - Thêm mới
- `UpdateAsync(id, entity)` - Cập nhật
- `UpdateFieldsAsync(id, updates)` - Cập nhật một số fields
- `DeleteAsync(id)` - Xóa
- `ExistsAsync(id)` - Kiểm tra tồn tại
- `CountAsync()` - Đếm số lượng
- `FindAsync(predicate)` - Tìm kiếm theo điều kiện

#### Specific Methods (theo từng repository):
- **IBookRepository:**
  - `GetByIsbnAsync(isbn)`
  - `GetBySlugAsync(slug)` 🆕
  - `GetByAuthorAsync(authorId)` 
  - `GetByCategoryAsync(category)`
  - `SearchByTitleAsync(title)`
  - `GetTopRatedAsync(count)`
  - `IncrementViewCountAsync(bookId)` 🆕
  - `GetByIdsAsync(bookIds)` 🆕
  - `UpdateByIsbnAsync(isbn, book)` 🆕
  - `DeleteByIsbnAsync(isbn)` 🆕

- **IUserRepository:**
  - `GetByEmailAsync(email)`
  - `GetByRoleAsync(role)`
  - `GetByStatusAsync(status)`
  - `AddAddressAsync(userId, address)` 🆕
  - `UpdateAddressAsync(userId, addressId, address)` 🆕
  - `DeleteAddressAsync(userId, addressId)` 🆕
  - `SetDefaultAddressAsync(userId, addressId)` 🆕
  - `AddToWishlistAsync(userId, bookId)` 🆕
  - `RemoveFromWishlistAsync(userId, bookId)` 🆕
  - `GetWishlistAsync(userId)` 🆕

- **IOrderRepository:**
  - `GetByUserIdAsync(userId)`
  - `GetByStatusAsync(status)`
  - `GetRecentOrdersAsync(count)`

- **IReviewRepository:**
  - `GetByBookIdAsync(bookId)`
  - `GetByUserIdAsync(userId)`
  - `GetAverageRatingByBookIdAsync(bookId)`

- **ICouponRepository:**
  - `GetByCodeAsync(code)`
  - `GetActiveAsync()`
  - `IncrementUsageAsync(id)`

## 📚 Collections Firebase

### 1. **books** - Quản lý sách
- ISBN, title, author, category
- Giá, stock, rating
- Mô tả chi tiết, ảnh bìa

### 2. **categories** - Danh mục sách
- Tên, icon, mô tả

### 3. **authors** - Tác giả
- Tên, bio, avatar

### 4. **users** - Người dùng
- Thông tin cá nhân
- Role (user/admin)
- Status (active/banned)
- Addresses (array) 🆕
- WishlistIds (array) 🆕

### 5. **orders** - Đơn hàng
- Thông tin khách hàng
- Danh sách sản phẩm
- Trạng thái đơn hàng
- Thanh toán

### 6. **reviews** - Đánh giá
- Rating, nội dung
- User, book liên quan

### 7. **ai_models** - Mô hình AI
- Danh sách các AI model
- Thông tin RPM, TPM, RPD

### 8. **coupons** - Mã giảm giá
- Code, giá trị giảm
- Điều kiện áp dụng
- Giới hạn sử dụng

### 9. **system_logs** - Nhật ký hệ thống
- Action, detail, status
- User thực hiện

### 10. **system_configs** - Cấu hình
- AI settings
- Các cấu hình khác

## 🔗 Tích Hợp với DigiBook Frontend

📖 **[INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md)** - Hướng dẫn chi tiết tích hợp Strategy & Decorator Patterns

**Quick Summary:**
- ✅ **Strategy Pattern**: Auto-pricing dựa trên membership tier (regular/member/vip)
- ✅ **Decorator Pattern**: Stack multiple discounts (membership + coupon + seasonal)
- ✅ **Endpoints Ready**: `/api/pricing/calculate-for-user/{userId}` & `/api/discount/calculate-checkout`
- ✅ **User Model Updated**: Added `membershipTier`, `totalSpent` fields

---

## 🔌 API Endpoints

### 📚 Books API (11 endpoints)

#### Basic CRUD (ISBN-based)
```http
GET    /api/books                  # Get all books
GET    /api/books/isbn/{isbn}      # Get book by ISBN
POST   /api/books                  # Create new book
PUT    /api/books/isbn/{isbn}      # Update book by ISBN
DELETE /api/books/isbn/{isbn}      # Delete book by ISBN
```

#### Advanced Queries
```http
GET  /api/books/test-connection     # Test Firebase connection
GET  /api/books/slug/{slug}         # Get book by slug (SEO) 🆕
GET  /api/books/author/{authorId}   # Get books by author
GET  /api/books/category/{category} # Get books by category
GET  /api/books/search?title={name} # Search books by title
GET  /api/books/top-rated?count=10  # Get top rated books
POST /api/books/by-ids              # Get multiple books by IDs (for wishlist) 🆕
```

### 💰 Pricing API (5 endpoints) 🆕

#### Strategy Pattern - Membership Pricing
```http
POST /api/pricing/calculate                    # Calculate price with specific strategy
POST /api/pricing/calculate-for-user/{userId}  # Calculate based on user membership ⭐
GET  /api/pricing/membership/{userId}          # Get membership info & benefits ⭐
POST /api/pricing/compare                      # Compare all pricing strategies
GET  /api/pricing/strategies                   # Get available strategies
```

### 🎁 Discount API (5 endpoints) 🆕

#### Decorator Pattern - Stack Multiple Discounts
```http
POST /api/discount/calculate              # Stack custom discounts
POST /api/discount/calculate-checkout     # Checkout total with stacked discounts ⭐
POST /api/discount/apply-coupon           # Apply & validate coupon
POST /api/discount/black-friday           # Example: Black Friday sale
GET  /api/discount/quick                  # Quick percentage discount
```

### 👤 Users API (16 endpoints)

#### Basic CRUD
```http
GET    /api/users                 # Get all users
GET    /api/users/{id}            # Get user by ID
GET    /api/users/email/{email}   # Get user by email
GET    /api/users/role/{role}     # Get users by role
GET    /api/users/status/{status} # Get users by status
POST   /api/users                 # Create new user
PUT    /api/users/{id}            # Update user
DELETE /api/users/{id}            # Delete user
```

#### Address Management 🆕
```http
POST   /api/users/{userId}/addresses                      # Add address
PUT    /api/users/{userId}/addresses/{addressId}          # Update address
DELETE /api/users/{userId}/addresses/{addressId}          # Delete address
PATCH  /api/users/{userId}/addresses/{addressId}/set-default  # Set default
```

#### Wishlist Management 🆕
```http
POST   /api/users/{userId}/wishlist/{bookId}  # Add to wishlist
DELETE /api/users/{userId}/wishlist/{bookId}  # Remove from wishlist
GET    /api/users/{userId}/wishlist           # Get wishlist IDs
```

#### Request Example (Create Book)
```json
POST /api/books
{
  "id": "978-XXXXXXXXXX",
  "title": "Clean Code",
  "author": "Robert C. Martin",
  "authorId": "author-001",
  "category": "Công nghệ",
  "price": 250000,
  "original_price": 300000,
  "stock_quantity": 100,
  "rating": 4.8,
  "cover": "https://...",
  "description": "...",
  "isbn": "978-XXXXXXXXXX",
  "pages": 464,
  "publisher": "Prentice Hall",
  "publishYear": 2008,
  "language": "Tiếng Việt",
  "badge": "Bán chạy"
}
```

### 📁 Categories API
```http
GET    /api/categories         # Get all categories
GET    /api/categories/{name}  # Get category by name
POST   /api/categories         # Create category
PUT    /api/categories/{name}  # Update category
DELETE /api/categories/{name}  # Delete category
```

## 🧪 Testing

Sử dụng Swagger UI để test API:
1. Chạy ứng dụng
2. Mở browser tại `https://localhost:7xxx/swagger`
3. Test endpoint `/api/books/test-connection` để kiểm tra kết nối Firebase

## 🛠️ Packages Sử Dụng

- **FirebaseAdmin** (3.0.0) - Firebase Admin SDK
- **Google.Cloud.Firestore** (3.8.0) - Firestore client
- **DotNetEnv** (3.0.0) - Load environment variables
- **Swashbuckle.AspNetCore** (6.5.0) - Swagger/OpenAPI

## 📝 Ghi chú

- API sử dụng CORS policy "AllowAll" cho development
- Tất cả responses có format:
  ```json
  {
    "success": true/false,
    "message": "...",
    "data": {...}
  }
  ```

## 🔒 Bảo mật

- File `.env` và `firebase-credentials.json` **KHÔNG BAO GIỜ** commit lên Git
- Thay đổi CORS policy khi deploy production
- Sử dụng environment variables cho production

## 📞 Liên hệ

Nếu có vấn đề, vui lòng tạo issue hoặc liên hệ team.

---

**Happy Coding! 🚀**
