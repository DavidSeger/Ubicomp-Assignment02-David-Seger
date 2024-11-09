import csv
import requests
import sys

# for testing purposes
def main():
    if len(sys.argv) != 3:
        print("Usage: python script.py <csv_filename> <server_url>")
        sys.exit(1)

    csv_filename = sys.argv[1]
    server_url = sys.argv[2]

    try:
        with open(csv_filename, 'r', newline='') as csvfile:
            reader = csv.reader(csvfile)
            for row in reader:
                body = ','.join(value for value in row)
                # Send PUT request with the body
                response = requests.put(server_url, data=body)
                # Check if the request was successful
                if response.status_code == 200:
                    print(f"Successfully sent: {body}")
                else:
                    print(f"Failed to send: {body}. Response code: {response.status_code}")
    except FileNotFoundError:
        print(f"Error: File {csv_filename} not found.")
        sys.exit(1)
    except requests.exceptions.RequestException as e:
        print(f"Request failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
