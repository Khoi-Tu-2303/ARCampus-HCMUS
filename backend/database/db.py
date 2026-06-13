import sqlite3
from pathlib import Path

DATABASE_NAME = str(Path(__file__).resolve().parents[1] / 'campus.db')


def get_connection():
    conn = sqlite3.connect(DATABASE_NAME, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    conn.execute('PRAGMA foreign_keys = ON;')
    return conn


def execute_query(query, params=()):
    conn = get_connection()
    try:
        cursor = conn.cursor()
        cursor.execute(query, params)
        conn.commit()
        return cursor.lastrowid
    finally:
        conn.close()


def fetch_one(query, params=()):
    conn = get_connection()
    try:
        cursor = conn.cursor()
        cursor.execute(query, params)
        return cursor.fetchone()
    finally:
        conn.close()


def fetch_all(query, params=()):
    conn = get_connection()
    try:
        cursor = conn.cursor()
        cursor.execute(query, params)
        return cursor.fetchall()
    finally:
        conn.close()
