# AGENTS.md

## Project overview

Đây là project có sẵn. Trước khi sửa code, hãy đọc cấu trúc thư mục và xác định các file liên quan.

## Repository structure

- `src/`: source code chính
- `tests/`: test
- `docs/`: tài liệu
- `scripts/`: script phụ trợ

## Development commands

- Install dependencies: điền lệnh ở đây nếu có
- Run app: điền lệnh ở đây nếu có
- Run tests: điền lệnh ở đây nếu có
- Build: điền lệnh ở đây nếu có

## Coding rules

- Không đổi architecture lớn nếu chưa được yêu cầu.
- Không xóa file hoặc đổi tên file nếu không cần thiết.
- Ưu tiên sửa nhỏ, rõ nguyên nhân.
- Khi sửa bug, hãy giải thích nguyên nhân bug.
- Khi thêm feature, hãy nêu file nào được sửa và vì sao.
- Không commit secret, API key, service account key, token hoặc password.

## Verification

Sau khi sửa code:

- Chạy test/lint/build nếu project có lệnh tương ứng.
- Nếu không thể chạy, hãy nói rõ lý do.
- Tóm tắt thay đổi cuối cùng theo file.

## Safety rules

- Không đọc, sửa, in ra hoặc commit nội dung file `.env`.
- Không đụng vào service account key, API key, token, password.
- Không chạy command xóa dữ liệu như `rm -rf`, `del /s`, `git reset --hard` nếu chưa được yêu cầu rõ.
- Trước khi sửa nhiều hơn 3 file, phải lập plan trước.
- Sau khi sửa, phải tóm tắt diff theo từng file.