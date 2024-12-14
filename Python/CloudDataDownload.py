print("Progress 0 Initializing...")

import requests
from bs4 import BeautifulSoup
from datetime import datetime, timezone
import os
import sys
import re

# Get the current date
current_date = datetime.now().strftime('%Y%m%d')

# Directory to save the file
save_directory = r"Data\Unzipped"

def download_file(url, filename):
    """Download the file from the given URL and save it in the Data directory."""
    if not os.path.exists(save_directory):
        os.makedirs(save_directory)

    filepath = os.path.join(save_directory, "cloudsoutput.grib2")
    print("Progress 50 Downloading cloud data...")
    print(f"Downloading {filename} from {url}...")

    response = requests.get(url, stream=True)
    if response.status_code == 200:
        with open(filepath, 'wb') as file:
            for chunk in response.iter_content(chunk_size=1024):
                file.write(chunk)
        print(f"Download completed: {filepath}")
        print("Progress 100 Done!")
    else:
        print(f"Failed to download {filename}")

# Archive Data Setup
def validate_timestamp_format(timestamp):
    """Validates if the timestamp is in the format YYYYMMDD-HH00."""
    pattern = r"^\d{8}-\d{4}$"  # Regex for YYYYMMDD-HH00
    return re.match(pattern, timestamp) is not None

def format_url(timestamp):
    """Formats the timestamp into the required URL."""
    date_part = timestamp.split('-')[0]  # Extract YYYYMMDD
    hh = timestamp[9:11]
    return f"https://noaa-rap-pds.s3.amazonaws.com/rap.{date_part}/rap.t{hh}z.awip32f00.grib2"

if len(sys.argv) > 1:
    timestamp = sys.argv[1]
    if validate_timestamp_format(timestamp):
        print(f"Valid timestamp: {timestamp}")
        base_url = format_url(timestamp)
        download_file(base_url, timestamp)
    else:
        print("Invalid timestamp format. Expected YYYYMMDD-HH00.")

else:
    # Current date
    base_url = f"https://nomads.ncep.noaa.gov/pub/data/nccf/com/rap/prod/rap.{current_date}/"

    def get_current_zulu_hour():
        """Return the current hour in UTC as an integer."""
        return int(datetime.now(timezone.utc).hour)

    def find_and_download_closest_file():
        current_hour = get_current_zulu_hour()
        
        # Get the index page
        print("Progress 20 Downloading cloud data index...")
        response = requests.get(base_url)
        if response.status_code != 200:
            print(f"Failed to access {base_url}")
            return

        # Parse the HTML content
        soup = BeautifulSoup(response.text, 'html.parser')
        links = soup.find_all('a')

        available_hours = []
        # Extract all available tXXz values from the links
        for link in links:
            href = link.get('href')
            if href and ".awip32f01.grib" in href:
                hour_str = href.split(".")[1][1:3]
                if hour_str.isdigit():
                    available_hours.append(int(hour_str))

        if not available_hours:
            print("No available files found.")
            return

        # Find the closest hour
        closest_hour = min(available_hours, key=lambda x: abs(x - current_hour))
        target_filename = f"rap.t{str(closest_hour).zfill(2)}z.awip32f01.grib2"
        
        # Download the closest file
        for link in links:
            href = link.get('href')
            if href and target_filename in href:
                download_url = base_url + href
                download_file(download_url, target_filename)
                break
        else:
            print(f"File {target_filename} not found.")

    if __name__ == "__main__":
        find_and_download_closest_file()


