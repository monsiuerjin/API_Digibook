# API_DigiBook - ASP.NET Core API với Firebase Firestore

API RESTful cho hệ thống quản lý sách điện tử DigiBook, sử dụng ASP.NET Core 6.0 và Firebase Firestore.

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
   ```

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
├── Controllers/          # API Controllers
│   ├── BooksController.cs
│   └── CategoriesController.cs
├── Models/              # Data Models
│   ├── Book.cs
│   ├── Author.cs
│   ├── Category.cs
│   ├── User.cs
│   ├── Order.cs
│   ├── Review.cs
│   ├── AIModel.cs
│   ├── Coupon.cs
│   ├── SystemLog.cs
│   └── SystemConfig.cs
├── Repositories/        # Repository Pattern
│   ├── IRepository.cs
│   ├── FirestoreRepository.cs
│   ├── IBookRepository.cs & BookRepository.cs
│   ├── ICategoryRepository.cs & CategoryRepository.cs
│   ├── IAuthorRepository.cs & AuthorRepository.cs
│   ├── IUserRepository.cs & UserRepository.cs
│   ├── IOrderRepository.cs & OrderRepository.cs
│   ├── IReviewRepository.cs & ReviewRepository.cs
│   └── ICouponRepository.cs & CouponRepository.cs
├── Services/            # Business Logic Services
│   └── FirebaseService.cs
├── Program.cs           # Entry point
├── .env                 # Environment variables (không commit)
└── firebase-credentials.json  # Firebase credentials (không commit)
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
  - `GetByAuthorAsync(authorId)` 
  - `GetByCategoryAsync(category)`
  - `SearchByTitleAsync(title)`
  - `GetTopRatedAsync(count)`

- **IUserRepository:**
  - `GetByEmailAsync(email)`
  - `GetByRoleAsync(role)`
  - `GetByStatusAsync(status)`

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

## 🔌 API Endpoints

### 📚 Books API

#### Basic CRUD
```http
GET    /api/books              # Get all books
GET    /api/books/{id}         # Get book by ID
POST   /api/books              # Create new book
PUT    /api/books/{id}         # Update book
DELETE /api/books/{id}         # Delete book
```

#### Advanced Queries
```http
GET /api/books/test-connection     # Test Firebase connection
GET /api/books/author/{authorId}   # Get books by author
GET /api/books/category/{category} # Get books by category
GET /api/books/search?title={name} # Search books by title
GET /api/books/top-rated?count=10  # Get top rated books
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
