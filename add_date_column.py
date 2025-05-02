import pandas as pd
import datetime
import numpy as np
import os

# File path
csv_path = "./InsightMCP/Data/results.csv"

# Create a backup of the original file
backup_path = csv_path + ".backup"
if not os.path.exists(backup_path):
    os.system(f"cp {csv_path} {backup_path}")
    print(f"Backup created at {backup_path}")

# Read the CSV file
df = pd.read_csv(csv_path)

# Get unique case numbers
unique_cases = df['CaseNumber'].unique()
num_cases = len(unique_cases)

# Generate dates from Jan 2023 to Dec 2024
start_date = datetime.datetime(2023, 1, 1)
date_range = pd.date_range(start=start_date, periods=24, freq='MS')  # Monthly start dates

# Map case numbers to dates
# If there are more case numbers than dates, we'll cycle through the dates
case_to_date = {}
for i, case in enumerate(unique_cases):
    date_idx = i % len(date_range)
    case_to_date[case] = date_range[date_idx].strftime('%Y-%m-%d')

# Add the date column
df['Date'] = df['CaseNumber'].map(case_to_date)

# Save the modified CSV
df.to_csv(csv_path, index=False)

print(f"Added Date column to {csv_path}")
print(f"Assigned {num_cases} unique case numbers to dates ranging from Jan 2023 to Dec 2024")

