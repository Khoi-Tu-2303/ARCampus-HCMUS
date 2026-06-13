# ARCampus-HCMUS

Ứng dụng hỗ trợ điều hướng thông minh trong khuôn viên Trường Đại học Khoa học Tự nhiên – ĐHQG TP.HCM bằng công nghệ Thực tế tăng cường (Augmented Reality - AR), kết hợp hệ thống Chatbot AI hỗ trợ tra cứu thông tin và tương tác với người dùng.

![Project Status](https://img.shields.io/badge/Status-Completed-success)
![Unity](https://img.shields.io/badge/Unity-2022+-black)
![C%23](https://img.shields.io/badge/C%23-Language-green)
![AR Foundation](https://img.shields.io/badge/AR-Foundation-blue)
![Firebase](https://img.shields.io/badge/Firebase-Backend-orange)
![Firestore](https://img.shields.io/badge/Firestore-Database-yellow)
![Android](https://img.shields.io/badge/Platform-Android-brightgreen)
![GPS](https://img.shields.io/badge/GPS-Navigation-red)
![Chatbot](https://img.shields.io/badge/AI-Chatbot-purple)
![License](https://img.shields.io/badge/License-Educational-lightgrey)

---

# Giới thiệu

ARCampus-HCMUS là dự án được phát triển nhằm hỗ trợ sinh viên, giảng viên và khách tham quan dễ dàng tìm kiếm địa điểm, định vị, điều hướng và tra cứu thông tin trong khuôn viên trường.

Hệ thống kết hợp nhiều công nghệ hiện đại:

* Augmented Reality (AR)
* GPS & Location Services
* Firebase Firestore
* Indoor Navigation
* Graph-Based Pathfinding
* AI Chatbot Assistant

giúp người dùng tiếp cận thông tin một cách trực quan và thuận tiện hơn.

---

# Chức năng chính

## Xác thực người dùng

* Đăng nhập.
* Đăng ký tài khoản.
* Đăng nhập khách (Guest).
* Phân quyền User và Staff.

## Điều hướng ngoài trời

* Xác định vị trí hiện tại bằng GPS.
* Tìm kiếm địa điểm trong khuôn viên trường.
* Tính toán đường đi tối ưu.
* Hiển thị lộ trình dẫn đường.

## Thực tế tăng cường (AR)

* Hiển thị nhãn AR cho các địa điểm.
* Hiển thị thông tin trực tiếp trên môi trường thực.
* Hỗ trợ định hướng trực quan thông qua camera thiết bị.

## Bản đồ trong nhà

* Hiển thị sơ đồ tầng.
* Tìm kiếm phòng học, đề xuất các địa điểm gần nhất với vị trí hiện tại của người dùng.
* Điều hướng trong các tòa nhà.

## Tra cứu thông tin địa điểm

* Thông tin tòa nhà.
* Thông tin phòng học.
* Thông tin cơ sở vật chất.

## Chatbot AI

* Quản lý hội thoại.
* Lưu lịch sử trò chuyện.
* Hỗ trợ tra cứu thông tin liên quan đến trường.
* Tương tác trực tiếp với người dùng thông qua giao diện chat.

## Chức năng dành cho Staff

* Quản lý thông tin người dùng.
* Cập nhật dữ liệu hệ thống.
* Quản lý nội dung phục vụ chatbot.

---

# Kiến trúc hệ thống

```text
                          ┌───────────────┐
                          │     User      │
                          └───────┬───────┘
                                  │
                                  ▼
                     ┌────────────────────────┐
                     │ Authentication Module  │
                     │ Login / Register       │
                     └──────────┬─────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
                ▼                               ▼
         ┌──────────────┐              ┌──────────────┐
         │ User Account │              │ Staff Account│
         └──────┬───────┘              └──────┬───────┘
                │                             │
                ▼                             ▼
      ┌─────────────────────┐      ┌──────────────────┐
      │     Main Module     │      │ Staff Management │
      └─────┬─────┬─────┬───┘      └──────────────────┘
            │     │     │
            │     │     │
            ▼     ▼     ▼
      ┌────────┐ ┌────────┐ ┌────────────┐
      │ AR     │ │ Indoor │ │ Chatbot    │
      │ Module │ │ Map    │ │ Module     │
      └────┬───┘ └────────┘ └─────┬──────┘
           │                       │
           ▼                       ▼
      ┌─────────────┐     ┌─────────────────┐
      │ Navigation  │     │ Chatbot Backend │
      └──────┬──────┘     └────────┬────────┘
             │                     │
             └─────────┬───────────┘
                       ▼
              ┌──────────────────┐
              │ Firebase Services│
              └──────────────────┘
```

---

# Luồng sử dụng

## Người dùng thông thường

```text
Login
   ↓
Main Scene
   ├── AR Navigation
   ├── Indoor Map
   ├── Search Location
   └── Chatbot
            ↓
   Conversation List
            ↓
        Chat Scene
```

## Nhân viên (Staff)

```text
Login
   ↓
Update Information Scene
```

---

# Công nghệ sử dụng

| Thành phần      | Công nghệ               |
| --------------- | ----------------------- |
| Engine          | Unity                   |
| Ngôn ngữ        | C#                      |
| AR Framework    | AR Foundation           |
| Cơ sở dữ liệu   | Firebase Firestore      |
| Xác thực        | Firebase Authentication |
| Định vị         | GPS                     |
| Điều hướng      | Graph-Based Navigation  |
| Chatbot Backend | REST API                |
| JSON Processing | Newtonsoft.Json         |
| Version Control | Git & GitHub            |

---

# Yêu cầu hệ thống

## Thiết bị

* Thiết bị Android có hỗ trợ nền tảng ARCore.
* Camera hoạt động ổn định.
* GPS và Dịch vụ định vị được bật.
* Kết nối Internet (Wifi/4G) để giao tiếp với AI Backend.
  
## Môi trường phát triển

**1. Unity (Frontend & AR):**
* Unity Editor (Dùng phiên bản **6000.4.7f1** để tránh xung đột).
* Android SDK & NDK (Cài đặt kèm qua Unity Hub).
* AR Foundation & ARCore XR Plugin.
* Firebase SDK.

**2. AI Backend:**
* Python 3.9 trở lên.
* FastAPI (API Server) & Uvicorn.
* [Ollama](https://ollama.com/) (Khởi chạy cục bộ mô hình ngôn ngữ lớn).

---
# Cài đặt dự án

## Clone Repository

```bash
git clone https://github.com/Khoi-Tu-2303/ARCampus-HCMUS.git
cd ARCampus-HCMUS
```

## Thiết lập Firebase (dùng chung cho cả Backend và Unity App)

### Bước 1: Tạo Firebase Project

Truy cập [Firebase Console](https://console.firebase.google.com), tạo project mới hoặc dùng project đã được cấu hình sẵn.

### Bước 2: Kích hoạt Firestore Database

```text
Build → Firestore Database
```

Tạo Firestore Database và nhập dữ liệu địa điểm theo cấu trúc của dự án.

### Bước 3: Kích hoạt Authentication

```text
Build → Authentication
```

Bật phương thức xác thực Email/Password.

### Bước 4: Lấy key cho Unity App

```text
Project Settings → General → Your apps → Add app (Android)
```

Tải file `google-services.json` và đặt vào:

```text
Assets/google-services.json
```

### Bước 5: Lấy key cho Backend

```text
Project Settings → Service Accounts → Generate New Private Key
```

Tải file JSON về, đổi tên (nếu cần) và đặt vào:

```text
backend/key/firebase_key.json
```

Cấu trúc thư mục:

```text
backend/
├── key/
│   └── firebase_key.json
├── .env
└── main.py
```

---

## Cấu hình Backend (.env)

Trong thư mục `backend`, tạo file `.env`:

```env
HF_TOKEN=<YOUR_HUGGINGFACE_TOKEN>
FIREBASE_CREDENTIALS_PATH=.\key\firebase_key.json
PORT=8000
```

| Biến môi trường | Mô tả |
|---|---|
| `HF_TOKEN` | Hugging Face Access Token dùng để truy cập mô hình AI |
| `FIREBASE_CREDENTIALS_PATH` | Đường dẫn tới file Firebase Service Account Key |
| `PORT` | Cổng chạy FastAPI Server |

### Lấy Hugging Face Token

1. Truy cập: `https://huggingface.co/settings/tokens`
2. Tạo Access Token mới.
3. Sao chép token và dán vào `.env`:

```env
HF_TOKEN=hf_xxxxxxxxxxxxxxxxxxxxx
```

### Lưu ý bảo mật

Không đưa các file sau lên GitHub:

```text
.env
firebase_key.json
```

Thêm vào `.gitignore`:

```gitignore
.env
key/firebase_key.json
```

---

## Khởi động AI Backend

### Bước 1: Khởi động Ollama

Mở ứng dụng Ollama trên máy tính, kiểm tra model:

```bash
ollama run qwen
```

Nếu model chưa có, Ollama sẽ tự tải về.

### Bước 2: Tạo môi trường Python

```bash
cd backend
python -m venv venv
```

Kích hoạt môi trường:

```bash
# Windows
venv\Scripts\activate

# Linux / macOS
source venv/bin/activate
```

### Bước 3: Cài đặt thư viện

```bash
pip install -r requirements.txt
```

### Bước 4: Tải mô hình NLU

Model được lưu trên Google Drive, cần tải về thủ công hoặc bằng `gdown`.

**Link tải model:**
https://drive.google.com/drive/folders/1JzU94a2Fm3KkbrHpA34UdWZu_OXNfLGn?usp=drive_link

#### Cách 1: Tải thủ công

Truy cập link Drive → tải toàn bộ thư mục model → giải nén nếu cần (.zip, .rar).

#### Cách 2: Tải bằng gdown (khuyến nghị)

```bash
pip install gdown
gdown --folder https://drive.google.com/drive/folders/1JzU94a2Fm3KkbrHpA34UdWZu_OXNfLGn
```

Sau khi tải xong, đặt model vào `backend/model/` theo cấu trúc:

```text
backend/
└── model/
    ├── intent_model/
    ├── tokenizer/
    └── config.json
```

### Bước 5: Khởi chạy Server

```bash
python app/main.py
```

---

## Lấy ứng dụng 

### Build từ Unity

#### Mở dự án Unity

1. Mở Unity Hub → chọn **Add Project** hoặc **Open**, trỏ tới thư mục dự án vừa clone.
2. Đảm bảo dùng đúng phiên bản: `Unity 6000.4.7f1` (khuyến nghị để tránh lỗi package và AR Foundation).

#### Đồng bộ Dependencies

```text
Assets → External Dependency Manager → Android Resolver → Force Resolver
```

Unity sẽ tự động tải các thư viện Android còn thiếu.

#### Cài đặt Newtonsoft JSON (nếu cần)

Nếu Console báo lỗi:

```text
The type or namespace name 'Newtonsoft' could not be found
```

Vào:

```text
Window → Package Manager → + → Add package by git URL...
```

Nhập:

```text
com.unity.nuget.newtonsoft-json
```

#### Build Android

```text
File → Build Profiles → Android
```

Nếu chưa cài Android Build Support:

```text
Unity Hub → Installs → Add Modules
```

Cài đặt:
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK


---

## Yêu cầu thiết bị khi chạy app

- Thiết bị Android có hỗ trợ ARCore.
- Camera hoạt động ổn định.
- GPS và Dịch vụ định vị được bật.
- Kết nối Internet (Wifi/4G) để giao tiếp với AI Backend.

---

# Thành viên nhóm

* Đinh Như Phát 
* Phạm Nguyên Phương
* Từ Văn Khôi
* Trần Lê Minh Đức 
---
## Liên hệ
Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ qua:
- **Email:** 241220**@student.hcmus.edu.vn
- **GitHub:** https://github.com/Khoi-Tu-2303 (Đại diện 1 người)
---
# Giấy phép

Dự án được phát triển phục vụ mục đích học tập, nghiên cứu và ứng dụng công nghệ trong môi trường giáo dục tại trường Đại học Khoa học tự nhiên - ĐHQG TP.HCM
