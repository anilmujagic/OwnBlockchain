namespace Own.Blockchain.Public.Data

type DbChange = {
    Number : int
    Script : string
}

module DbChanges =

    let internal firebirdChanges : DbChange list =
        [
            {
                Number = 1
                Script =
                    """
                    CREATE TABLE db_version (
                        version_number INTEGER NOT NULL,
                        execution_timestamp BIGINT NOT NULL,

                        CONSTRAINT db_version__pk PRIMARY KEY (version_number)
                    );
                    """
            }
            {
                Number = 2
                Script =
                    """
                    CREATE TABLE tx (
                        tx_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        tx_hash VARCHAR(50) NOT NULL,
                        sender_address VARCHAR(50) NOT NULL,
                        nonce BIGINT NOT NULL,
                        fee DECIMAL(18, 8) NOT NULL,
                        action_count SMALLINT NOT NULL,

                        CONSTRAINT tx__pk PRIMARY KEY (tx_id),
                        CONSTRAINT tx__uk__tx_hash UNIQUE (tx_hash)
                    );
                    CREATE INDEX tx__ix__sender_address ON tx (sender_address);
                    CREATE DESCENDING INDEX tx__ix__fee ON tx (fee);

                    CREATE TABLE chx_balance (
                        chx_balance_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        blockchain_address VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,
                        nonce BIGINT NOT NULL,

                        CONSTRAINT chx_balance__pk PRIMARY KEY (chx_balance_id),
                        CONSTRAINT chx_balance__uk__address UNIQUE (blockchain_address)
                    );

                    CREATE TABLE account (
                        account_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        account_hash VARCHAR(50) NOT NULL,
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT account__pk PRIMARY KEY (account_id),
                        CONSTRAINT account__uk__account_hash UNIQUE (account_hash)
                    );

                    CREATE TABLE asset (
                        asset_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        asset_hash VARCHAR(50) NOT NULL,
                        asset_code VARCHAR(20),
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT asset__pk PRIMARY KEY (asset_id),
                        CONSTRAINT asset__uk__asset_hash UNIQUE (asset_hash),
                        CONSTRAINT asset__uk__asset_code UNIQUE (asset_code)
                    );

                    CREATE TABLE holding (
                        holding_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        account_id BIGINT NOT NULL,
                        asset_hash VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,

                        CONSTRAINT holding__pk PRIMARY KEY (holding_id),
                        CONSTRAINT holding__uk__acc_id__ast_hash UNIQUE (account_id, asset_hash),
                        CONSTRAINT holding__fk__account FOREIGN KEY (account_id)
                            REFERENCES account (account_id)
                    );

                    CREATE TABLE block (
                        block_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        block_number BIGINT NOT NULL,
                        block_hash VARCHAR(50) NOT NULL,
                        block_timestamp BIGINT NOT NULL,
                        is_config_block BOOLEAN NOT NULL,
                        is_applied BOOLEAN NOT NULL,

                        CONSTRAINT block__pk PRIMARY KEY (block_id),
                        CONSTRAINT block__uk__number UNIQUE (block_number),
                        CONSTRAINT block__uk__hash UNIQUE (block_hash),
                        CONSTRAINT block__uk__timestamp UNIQUE (block_timestamp)
                    );
                    """
            }
            {
                Number = 3
                Script =
                    """
                    CREATE TABLE validator (
                        validator_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        validator_address VARCHAR(50) NOT NULL,
                        network_address VARCHAR(250) NOT NULL,
                        shared_reward_percent DECIMAL(5, 2) NOT NULL,

                        CONSTRAINT validator__pk PRIMARY KEY (validator_id),
                        CONSTRAINT validator__uk__val_addr UNIQUE (validator_address)
                    );

                    CREATE TABLE stake (
                        stake_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        staker_address VARCHAR(50) NOT NULL,
                        validator_address VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,

                        CONSTRAINT stake__pk PRIMARY KEY (stake_id),
                        CONSTRAINT stake__uk__staker__validator
                            UNIQUE (staker_address, validator_address)
                    );

                    CREATE TABLE peer (
                        peer_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        network_address VARCHAR(250) NOT NULL,

                        CONSTRAINT peer__pk PRIMARY KEY (peer_id),
                        CONSTRAINT peer__uk__network_address UNIQUE (network_address)
                    );
                    """
            }
            {
                Number = 4
                Script =
                    """
                    CREATE TABLE vote (
                        vote_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        holding_id BIGINT NOT NULL,
                        resolution_hash VARCHAR(50) NOT NULL,
                        vote_hash VARCHAR(50) NOT NULL,
                        vote_weight DECIMAL(18, 8),

                        CONSTRAINT vote__pk PRIMARY KEY (vote_id),
                        CONSTRAINT vote__uk__holding__resolution UNIQUE (holding_id, resolution_hash),
                        CONSTRAINT vote__fk__holding FOREIGN KEY (holding_id)
                            REFERENCES holding (holding_id)
                    );

                    CREATE TABLE kyc_controller (
                        kyc_controller_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        asset_id BIGINT NOT NULL,
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT kyc_controller__pk PRIMARY KEY (kyc_controller_id),
                        CONSTRAINT kyc_controller__uk__asset__ctrl UNIQUE (asset_id, controller_address),
                        CONSTRAINT kyc_controller__fk__asset FOREIGN KEY (asset_id)
                            REFERENCES asset (asset_id)
                    );

                    CREATE TABLE eligibility (
                        eligibility_id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                        account_id BIGINT NOT NULL,
                        asset_id BIGINT NOT NULL,
                        is_eligible BOOLEAN NOT NULL,
                        is_transferable BOOLEAN NOT NULL,
                        kyc_controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT eligibility__pk PRIMARY KEY (eligibility_id),
                        CONSTRAINT eligibility__uk__account__asset UNIQUE (account_id, asset_id),
                        CONSTRAINT eligibility__fk__account FOREIGN KEY (account_id)
                            REFERENCES account (account_id),
                        CONSTRAINT eligibility__fk__asset FOREIGN KEY (asset_id)
                            REFERENCES asset (asset_id)
                    );
                    CREATE INDEX eligibility__ix__asset_id ON eligibility (asset_id);
                    """
            }
        ]

    let internal postgresqlChanges : DbChange list =
        [
            {
                Number = 1
                Script =
                    """
                    CREATE TABLE db_version (
                        version_number INTEGER NOT NULL,
                        execution_timestamp BIGINT NOT NULL,

                        CONSTRAINT db_version__pk PRIMARY KEY (version_number)
                    );
                    """
            }
            {
                Number = 2
                Script =
                    """
                    CREATE TABLE tx (
                        tx_id BIGSERIAL NOT NULL,
                        tx_hash VARCHAR(50) NOT NULL,
                        sender_address VARCHAR(50) NOT NULL,
                        nonce BIGINT NOT NULL,
                        fee DECIMAL(18, 8) NOT NULL,
                        action_count SMALLINT NOT NULL,

                        CONSTRAINT tx__pk PRIMARY KEY (tx_id),
                        CONSTRAINT tx__uk__tx_hash UNIQUE (tx_hash)
                    );
                    CREATE INDEX tx__ix__sender_address ON tx (sender_address);
                    CREATE INDEX tx__ix__fee ON tx (fee DESC);

                    CREATE TABLE chx_balance (
                        chx_balance_id BIGSERIAL NOT NULL,
                        blockchain_address VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,
                        nonce BIGINT NOT NULL,

                        CONSTRAINT chx_balance__pk PRIMARY KEY (chx_balance_id),
                        CONSTRAINT chx_balance__uk__blockchain_address UNIQUE (blockchain_address)
                    );

                    CREATE TABLE account (
                        account_id BIGSERIAL NOT NULL,
                        account_hash VARCHAR(50) NOT NULL,
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT account__pk PRIMARY KEY (account_id),
                        CONSTRAINT account__uk__account_hash UNIQUE (account_hash)
                    );

                    CREATE TABLE asset (
                        asset_id BIGSERIAL NOT NULL,
                        asset_hash VARCHAR(50) NOT NULL,
                        asset_code VARCHAR(20),
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT asset__pk PRIMARY KEY (asset_id),
                        CONSTRAINT asset__uk__asset_hash UNIQUE (asset_hash),
                        CONSTRAINT asset__uk__asset_code UNIQUE (asset_code)
                    );

                    CREATE TABLE holding (
                        holding_id BIGSERIAL NOT NULL,
                        account_id BIGINT NOT NULL,
                        asset_hash VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,

                        CONSTRAINT holding__pk PRIMARY KEY (holding_id),
                        CONSTRAINT holding__uk__account_id__asset_hash UNIQUE (account_id, asset_hash),
                        CONSTRAINT holding__fk__account FOREIGN KEY (account_id)
                            REFERENCES account (account_id)
                    );

                    CREATE TABLE block (
                        block_id BIGSERIAL NOT NULL,
                        block_number BIGINT NOT NULL,
                        block_hash VARCHAR(50) NOT NULL,
                        block_timestamp BIGINT NOT NULL,
                        is_config_block BOOLEAN NOT NULL,
                        is_applied BOOLEAN NOT NULL,

                        CONSTRAINT block__pk PRIMARY KEY (block_id),
                        CONSTRAINT block__uk__number UNIQUE (block_number),
                        CONSTRAINT block__uk__hash UNIQUE (block_hash),
                        CONSTRAINT block__uk__timestamp UNIQUE (block_timestamp)
                    );
                    """
            }
            {
                Number = 3
                Script =
                    """
                    CREATE TABLE validator (
                        validator_id BIGSERIAL NOT NULL,
                        validator_address VARCHAR(50) NOT NULL,
                        network_address VARCHAR(250) NOT NULL,
                        shared_reward_percent DECIMAL(5, 2) NOT NULL,

                        CONSTRAINT validator__pk PRIMARY KEY (validator_id),
                        CONSTRAINT validator__uk__validator_address UNIQUE (validator_address)
                    );

                    CREATE TABLE stake (
                        stake_id BIGSERIAL NOT NULL,
                        staker_address VARCHAR(50) NOT NULL,
                        validator_address VARCHAR(50) NOT NULL,
                        amount DECIMAL(18, 8) NOT NULL,

                        CONSTRAINT stake__pk PRIMARY KEY (stake_id),
                        CONSTRAINT stake__uk__staker_address__validator_address
                            UNIQUE (staker_address, validator_address)
                    );

                    CREATE TABLE peer (
                        peer_id BIGSERIAL NOT NULL,
                        network_address VARCHAR(250) NOT NULL,

                        CONSTRAINT peer__pk PRIMARY KEY (peer_id),
                        CONSTRAINT peer__uk__network_address UNIQUE (network_address)
                    );
                    """
            }
            {
                Number = 4
                Script =
                    """
                    CREATE TABLE vote (
                        vote_id BIGSERIAL NOT NULL,
                        holding_id BIGINT NOT NULL,
                        resolution_hash VARCHAR(50) NOT NULL,
                        vote_hash VARCHAR(50) NOT NULL,
                        vote_weight DECIMAL(18, 8),

                        CONSTRAINT vote__pk PRIMARY KEY (vote_id),
                        CONSTRAINT vote__uk__holding_id__resolution_hash UNIQUE (holding_id, resolution_hash),
                        CONSTRAINT vote__fk__holding FOREIGN KEY (holding_id)
                            REFERENCES holding (holding_id)
                    );

                    CREATE TABLE kyc_controller (
                        kyc_controller_id BIGSERIAL NOT NULL,
                        asset_id BIGINT NOT NULL,
                        controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT kyc_controller__pk PRIMARY KEY (kyc_controller_id),
                        CONSTRAINT kyc_controller__uk__asset__ctrl UNIQUE (asset_id, controller_address),
                        CONSTRAINT kyc_controller__fk__asset FOREIGN KEY (asset_id)
                            REFERENCES asset (asset_id)
                    );

                    CREATE TABLE eligibility (
                        eligibility_id BIGSERIAL NOT NULL,
                        account_id BIGINT NOT NULL,
                        asset_id BIGINT NOT NULL,
                        is_eligible BOOLEAN NOT NULL,
                        is_transferable BOOLEAN NOT NULL,
                        kyc_controller_address VARCHAR(50) NOT NULL,

                        CONSTRAINT eligibility__pk PRIMARY KEY (eligibility_id),
                        CONSTRAINT eligibility__uk__account__asset UNIQUE (account_id, asset_id),
                        CONSTRAINT eligibility__fk__account FOREIGN KEY (account_id)
                            REFERENCES account (account_id),
                        CONSTRAINT eligibility__fk__asset FOREIGN KEY (asset_id)
                            REFERENCES asset (asset_id)
                    );
                    CREATE INDEX eligibility__ix__asset_id ON eligibility (asset_id);
                    """
            }
        ]
