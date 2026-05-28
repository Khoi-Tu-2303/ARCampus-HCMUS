from sqlite3 import IntegrityError
from database.db import fetch_one, execute_query
from app.utils.security import hash_password, verify_password


def register_user(username: str, password: str, full_name: str, student_id: str):
    if not is_valid(password):
        return None, (400, {'error': 'Invalid password'})

    existing = fetch_one('SELECT id FROM users WHERE username = ?', (username,))
    if existing:
        return None, (409, {'error': 'Username already exists'})

    password_hash = hash_password(password)

    try:
        user_id = execute_query(
            'INSERT INTO users (username, password_hash, full_name, student_id) VALUES (?, ?, ?, ?)',
            (username, password_hash, full_name, student_id),
        )
        # Lấy role student
        role = fetch_one(
            'SELECT id, name FROM roles WHERE name = ?',
            ('student',)
        )
        # Gán role cho user
        execute_query(
            '''
            INSERT INTO user_roles (user_id, role_id)
            VALUES (?, ?)
            ''',
            (user_id, role['id'])
        )
        return {
            'id': user_id,
            'username': username,
            'user_role': role['name']
        }, None
    except IntegrityError:
        # Covers rare unique collisions such as duplicate student_id.
        return None, (409, {'error': 'Username already exists'})
    except Exception:
        return None, (500, {'error': 'Internal server error'})


def login_user(username: str, password: str):
    row = fetch_one(
        '''
        SELECT 
            u.id,
            u.username,
            u.password_hash,
            r.name AS user_role
        FROM users u
        LEFT JOIN user_roles ur ON u.id = ur.user_id
        LEFT JOIN roles r ON ur.role_id = r.id
        WHERE u.username = ?
        ''',
        (username,)
    )
    if not row:
        return None, (404, {'error': 'Username does not exist'})

    if not verify_password(password, row['password_hash']):
        return None, (401, {'error': 'Incorrect password'})

    return {
        'id': row['id'],
        'username': row['username'],
        'user_role': row['user_role']
    }, None

def register_staff(username: str, password: str, full_name: str):
    password_hash = hash_password(password)

    user_id = execute_query(
        '''
        INSERT INTO users (username, password_hash, full_name)
        VALUES (?, ?, ?)
        ''',
        (username, password_hash, full_name)
    )

    role = fetch_one(
        'SELECT id FROM roles WHERE name = ?',
        ('staff',)
    )

    execute_query(
        '''
        INSERT INTO user_roles (user_id, role_id)
        VALUES (?, ?)
        ''',
        (user_id, role['id'])
    )

    return {
        'id': user_id,
        'username': username,
        'user_role': 'staff'
    }
    
def is_valid(password : str):
    return len(password) >= 6

if __name__ == "__main__":
    register_staff(username="stafftest1", password="123456", full_name="Staff 1")