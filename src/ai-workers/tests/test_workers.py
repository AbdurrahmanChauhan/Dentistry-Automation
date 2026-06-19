import pytest
from fastapi.testclient import TestClient
from workers.main import app

client = TestClient(app)


def test_health():
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "healthy"


def test_denial_summarize():
    r = client.post("/summarize/denial", json={
        "carcCode": "CO-45",
        "claimContext": "Claim CLM-001",
        "billedAmount": 75,
        "paidAmount": 60,
    })
    assert r.status_code == 200
    data = r.json()
    assert "summary" in data
    assert "suggestedAction" in data
    assert data["priorityScore"] > 0


def test_eob_extract():
    r = client.post(
        "/extract/eob",
        files={"file": ("test.txt", b"D0120 $75.00 $60.00 D1110 $125.00 $100.00", "text/plain")},
    )
    assert r.status_code == 200
    data = r.json()
    assert data["success"]
    assert len(data["lines"]) > 0
