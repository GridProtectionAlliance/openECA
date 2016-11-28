%% Line Parameter Estimation
% clc
% clear all
% clc

%% Conduct line parameter estimation based on two bus equipped with accurate PMUs
% Use Breadth-first-search to estimate the ratio erros of each transmission
% line one by one

%% System Configuration
% two accurate KV-KI buses
accurate_bus=81;
original_accurate_line_number = 10;

% system information
AC_line_info_struct=load('AC_line_info_true_value_Zy.mat');
AC_line_info = AC_line_info_struct.AC_line_info;

% use accurate data
% AC_line_info(:,5) = ones(size(AC_line_info,1),1);
% AC_line_info(:,6) = ones(size(AC_line_info,1),1);
% AC_line_info(:,8) = ones(size(AC_line_info,1),1);
% AC_line_info(:,9) = ones(size(AC_line_info,1),1);

line_bus_info_all_connections = [AC_line_info(:,1), AC_line_info(:,2), AC_line_info(:,4), AC_line_info(:,7)];

% accurate bus/line data processing

%----------------------------------------------------%
line_name=['line_',num2str(original_accurate_line_number), '_measured_positive_sequence.mat'];
VI_origin_struct=load(line_name);
VI_measurement_set = VI_origin_struct.VI_measurement_set;
line_name=['line_',num2str(original_accurate_line_number),'_measured_positive_sequence_origin.mat'];
save(line_name,'VI_measurement_set');
%----------------------------------------------------%

line_name=['line_' ,num2str(original_accurate_line_number), '_measured_positive_sequence_origin.mat'];
VI_origin_struct=load(line_name);
VI_origin = VI_origin_struct.VI_measurement_set;

line_name_true=['line_' ,num2str(original_accurate_line_number), '_true_positive_sequence.mat'];
VI_origin_struct=load(line_name_true);
VI_origin_true = VI_origin_struct.VI_true_set;

VI_origin(:,3) = VI_origin_true(:,3);%quant(real(VI_origin_true(:,1)),20) + 1i*quant(imag(VI_origin_true(:,1)),20);
VI_origin(:,4) = VI_origin_true(:,4);% VI_origin(:,2) = quant(real(VI_origin_true(:,2)),0.65) + 1i*quant(imag(VI_origin_true(:,2)),0.65);

original_KV1 = 1;
original_KI1 = 1;
AC_line_info(original_accurate_line_number,8) = 1;
AC_line_info(original_accurate_line_number,9) = 1;
% original_KI1 = 1;
% AC_line_info(original_accurate_line_number,6) = 1;
VI_measurement_set = VI_origin;
line_name=['line_' ,num2str(original_accurate_line_number), '_measured_positive_sequence.mat'];
save(line_name,'VI_measurement_set');
% VI_true_set = VI_origin;
% save(line_name,'VI_true_set');

Bus_number_set_345_struct = load('Bus_number_set_345KV.mat');
Bus_number_set_345KV = Bus_number_set_345_struct.bus_number_set;

% 345KV subsystem lines
line_bus_info_345KV_struct = load('line_bus_info_345KV.mat');
line_bus_info_345KV = line_bus_info_345KV_struct.line_bus_info_345KV;

line_number_set_345KV =[];
% for idx=1:size(line_bus_info_all_connections,1) 
%     if sum(ismember(line_bus_info_500KV(:,1:2),line_bus_info_all_connections(idx,3:4),'rows'))
%         line_number_set_500KV = [line_number_set_500KV;line_bus_info_all_connections(idx,1)];
%     elseif sum(ismember([line_bus_info_500KV(:,2), line_bus_info_500KV(:,1)],line_bus_info_all_connections(idx,3:4),'rows'))
%         line_number_set_500KV = [line_number_set_500KV;line_bus_info_all_connections(idx,1)];
%     end
% end
for idx=1:size(line_bus_info_all_connections,1) 
    if sum(ismember(line_bus_info_345KV,[line_bus_info_all_connections(idx,3:4),line_bus_info_all_connections(idx,2)],'rows'))
        line_number_set_345KV = [line_number_set_345KV;line_bus_info_all_connections(idx,1)];
    elseif sum(ismember([line_bus_info_345KV(:,2), line_bus_info_345KV(:,1),line_bus_info_345KV(:,3)],[line_bus_info_all_connections(idx,3:4),line_bus_info_all_connections(idx,2)],'rows'))
        line_number_set_345KV = [line_number_set_345KV;line_bus_info_all_connections(idx,1)];
    end
end
line_bus_info_345KV = [line_number_set_345KV, line_bus_info_345KV];
line_number_set=line_bus_info_345KV(:,1);

connection_matrix = system_345KV_connection_analysis_IEEE_118(line_bus_info_345KV, Bus_number_set_345KV);

bus_num=size(connection_matrix,1);
line_num=size(line_number_set,1);

visited=zeros(line_num,2); % [line number, flag]record the search for all the lines not buses
visited(:,1) = line_number_set;

line_estimation_results = zeros(line_num,9);
%[line number, from bus number, KV1, KI1, to bus number, KV2, KI2, Z, y]
line_estimation_results(:,1) = line_bus_info_345KV(:,1);
line_estimation_results(:,8:9) = AC_line_info(line_estimation_results(:,1),10:11);
accurate_line_index = find(line_estimation_results(:,1)==original_accurate_line_number);
line_estimation_results(accurate_line_index,2:4) = [accurate_bus, original_KV1, original_KI1];
record_components=zeros;

for i=1:size(accurate_bus,1)
    
    %%topology analysis, BFS
    connected_components = zeros; 
    %record current connected component [from bus number, to bus number, line number], ...    
    %original bus - [accurate bus number, accurate bus number, 0]
    %start bus of every layer - [parent bus number, start bus number, 0]
    
    %temp_component = [accurate_bus(i,1), accurate_bus(i,1), 0];
    temp_component = [0, 0, 0];
    [visited,connected_components]=...
            breadth_first_search(connection_matrix,[accurate_bus(i,1), accurate_bus(i,1)],temp_component,...
            visited,Bus_number_set_345KV,line_bus_info_345KV, accurate_bus, original_accurate_line_number);
    connected_components = connected_components(connected_components(:,1)~=0,:);
    
    idx_unvisited = find(visited(:,2)==0);
    unvisited_line = visited(idx_unvisited,1);
    for idx1=1:size(idx_unvisited,1)
        temp_unvisited_line = unvisited_line(idx1,1);
        temp_unvisited_from_bus = line_bus_info_345KV(line_bus_info_345KV(:,1)==temp_unvisited_line,2);
        temp_unvisited_to_bus = line_bus_info_345KV(line_bus_info_345KV(:,1)==temp_unvisited_line,3);

        temp_line_bus_info1 = [temp_unvisited_from_bus, temp_unvisited_to_bus];
        location1 = min(find(ismember(connected_components(:,1:2),temp_line_bus_info1, 'rows')));  
        temp_line_bus_info2 = [temp_unvisited_to_bus, temp_unvisited_from_bus];
        location2 = min(find(ismember(connected_components(:,1:2),temp_line_bus_info2, 'rows')));
        
        if ~isempty(location1)
            location = location1;
            connected_components = [connected_components(1:location,:); ...
                temp_line_bus_info1, temp_unvisited_line;...
                connected_components((location+1):end,:)];  
        elseif ~isempty(location2)
            location = location2;
            connected_components = [connected_components(1:location,:); ...
                temp_line_bus_info2, temp_unvisited_line;...
                connected_components((location+1):end,:)]; 
        end
    end        
        
    %%start estimation based on topology
    current_accurate_bus_number = accurate_bus(i,1);
    calibration_history = [];
    for j=1:size(connected_components,1)%line_num
        if connected_components(j,3)==0 % layer start bus case
            
            if connected_components(j,1)==connected_components(j,2)% find the original accurate bus
%                 current_line_number = original_accurate_line_number;
%                 current_index = find(line_estimation_results(:,1)==current_line_number);
%                 current_KV = line_estimation_results(current_index,3);
%                 current_KI = line_estimation_results(current_index,4);
%                 current_accurate_line_number = original_accurate_line_number;
                continue;
            else
                current_line_number = find_line_from_buses(connected_components(j,1),connected_components(j,2),line_bus_info_345KV);
                current_index = find(line_estimation_results(:,1)==current_line_number);
                current_KV = line_estimation_results(current_index,6);
                current_KI = line_estimation_results(current_index,7);
                current_accurate_line_number = current_line_number;
            end
            
            % find all injections of current bus
            current_bus_number = connected_components(j,2); 
            
            if current_bus_number == 314904
                a=1;
            end
            
            current_injection_line_number_set = find_injections(current_bus_number,line_bus_info_all_connections);
            if size(current_injection_line_number_set,1)==1
                continue;
            end
            
%             current_KI_injection_set = single_bus_injections_ratio_errors_estimation(current_bus_number,current_injection_line_number_set,...
%                 current_accurate_line_number,current_KI,calibration_history,line_estimation_results);

            current_KI_injection_set = single_bus_injections_ratio_errors_estimation_inequality_bounds(current_bus_number,current_injection_line_number_set,...
                current_accurate_line_number,current_KI,calibration_history,line_estimation_results);
            
            current_KV_injection_set = single_bus_voltage_correction_popagation(current_bus_number,current_injection_line_number_set,...
                current_accurate_line_number,current_KV,calibration_history,line_estimation_results);
            
            % record estimation results in results set
%             current_inaccurate_line_numbers = current_injection_line_number_set...
%                 (current_injection_line_number_set(:,1)~=current_accurate_line_number,:);
            current_inaccurate_line_numbers = current_KI_injection_set(:,1);
            for k=1:size(current_inaccurate_line_numbers,1)
                results_set_index = find(line_estimation_results(:,1)==current_inaccurate_line_numbers(k,1));
                if ~isempty(results_set_index)                    
                    line_estimation_results(results_set_index,2) = current_bus_number;
                    line_estimation_results(results_set_index,3) = current_KV_injection_set(k,2);
                    line_estimation_results(results_set_index,4) = current_KI_injection_set(k,2);
                end
            end            
        else % general estimation process
            current_line_number = connected_components(j,3);
            if current_line_number == 30
                a=1;
            end
            results_set_index = find(line_estimation_results(:,1)==current_line_number);
            current_bus_number = line_estimation_results(results_set_index,2);
            KV1 = line_estimation_results(results_set_index,3);
            KI1 = line_estimation_results(results_set_index,4);                                 
            
            [ZThat_per_unit_mean, YThat_per_unit_mean,KV2,KI2]=single_line_parameter_estimation...
                (current_line_number,current_bus_number,KV1,KI1,line_bus_info_345KV(results_set_index,2:3));
            
            line_estimation_results(results_set_index,5) = connected_components(j,2);
            line_estimation_results(results_set_index,6) = KV2;
            line_estimation_results(results_set_index,7) = KI2;
            line_estimation_results(results_set_index,8) = ZThat_per_unit_mean;
            line_estimation_results(results_set_index,9) = YThat_per_unit_mean;
            
            calibration_history = [calibration_history; current_line_number];
        end
    end
       
end

save('line_estimation_results.mat','line_estimation_results');

for idx2 = 1:size(line_estimation_results,1)
    if line_estimation_results(idx2,2) == AC_line_info(line_estimation_results(idx2,1),4)
        KV1_error(idx2,1) = line_estimation_results(idx2,3)- AC_line_info(line_estimation_results(idx2,1),5);
        KI1_error(idx2,1) = line_estimation_results(idx2,4)- AC_line_info(line_estimation_results(idx2,1),6);

        KV2_error(idx2,1) = line_estimation_results(idx2,6)- AC_line_info(line_estimation_results(idx2,1),8);
        KI2_error(idx2,1) = line_estimation_results(idx2,7)- AC_line_info(line_estimation_results(idx2,1),9);
    else
        KV1_error(idx2,1) = line_estimation_results(idx2,3)- AC_line_info(line_estimation_results(idx2,1),8);
        KI1_error(idx2,1) = line_estimation_results(idx2,4)- AC_line_info(line_estimation_results(idx2,1),9);

        KV2_error(idx2,1) = line_estimation_results(idx2,6)- AC_line_info(line_estimation_results(idx2,1),5);
        KI2_error(idx2,1) = line_estimation_results(idx2,7)- AC_line_info(line_estimation_results(idx2,1),6);
    end
end

Z_pu_error = line_estimation_results(:,8) - AC_line_info(line_estimation_results(:,1),10);

visiting_order = connected_components(find(connected_components(:,3)~=0),3);

for idx = 1: size(visiting_order,1)

    KV1_error_visiting_order(idx,1) = KV1_error(find(line_estimation_results(:,1)==visiting_order(idx,1)),1);
    KI1_error_visiting_order(idx,1) = KI1_error(find(line_estimation_results(:,1)==visiting_order(idx,1)),1);
    KV2_error_visiting_order(idx,1) = KV2_error(find(line_estimation_results(:,1)==visiting_order(idx,1)),1);
    KI2_error_visiting_order(idx,1) = KI2_error(find(line_estimation_results(:,1)==visiting_order(idx,1)),1);

    Z_pu_error_visiting_order(idx,1) = Z_pu_error(find(line_estimation_results(:,1)==visiting_order(idx,1)),1);

end

KV1_error=KV1_error_visiting_order;
KI1_error=KI1_error_visiting_order;
KV2_error=KV2_error_visiting_order;
KI2_error=KI2_error_visiting_order;
Z_pu_error = Z_pu_error_visiting_order;

line_number = visiting_order;

table(line_number, KV1_error,KI1_error,KV2_error,KI2_error,Z_pu_error)





